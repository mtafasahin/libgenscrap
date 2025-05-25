using BookApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

public class BooksModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ScrapingHub> _hubContext;

    public BooksModel(IConfiguration configuration, IHubContext<ScrapingHub> hubContext)
    {
        _configuration = configuration;
        _hubContext = hubContext;
    }

    public List<BookDto> Books { get; set; } = new();
    public List<string> AvailableLanguages { get; set; } = new();
    public List<string> AvailableYears { get; set; } = new();

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string? SearchTitle { get; set; }
    public string? Language { get; set; }
    public string? Year { get; set; }

    private static List<string>? _cachedLanguages;
    private static List<string>? _cachedYears;

    public string ViewMode { get; set; } = "card";

    public void OnGet(int pageNumber = 1, int pageSize = 24, string? searchTitle = null, string? language = null, string? year = null,
            string? viewMode = "card"
        )
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        SearchTitle = searchTitle;
        Language = language;
        Year = year;
        ViewMode = viewMode;

        var baseFolder = _configuration["Database:BaseFolder"] ?? "libgenscrap";
        var fileName = _configuration["Database:FileName"] ?? "libgen.db";
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), baseFolder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        // Dropdown verileri
        if (_cachedLanguages == null || _cachedYears == null)
        {
            _cachedLanguages = QueryColumnDistinct(conn, "Language");
            _cachedYears = QueryColumnDistinct(conn, "Year");
        }

        AvailableLanguages = _cachedLanguages!;
        AvailableYears = _cachedYears!;

        var ftsClause = string.IsNullOrWhiteSpace(searchTitle)
        ? ""
        : $"AND BooksFTS MATCH '{EscapeFtsQuery(searchTitle)}*'";
        // Toplam kayıt sayısı
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $@"
            SELECT COUNT(*) 
            FROM Books 
            JOIN BooksFTS ON Books.Id = BooksFTS.rowid
            WHERE 1=1
            {ftsClause}
            AND (@language IS NULL OR Books.Language = @language)
            AND (@year IS NULL OR Books.Year = @year)";
        countCmd.Parameters.AddWithValue("@searchTitle", (object?)searchTitle ?? DBNull.Value);
        countCmd.Parameters.AddWithValue("@ftsQuery", string.IsNullOrEmpty(searchTitle) ? DBNull.Value : $"{searchTitle}*");
        countCmd.Parameters.AddWithValue("@language", (object?)language ?? DBNull.Value);
        countCmd.Parameters.AddWithValue("@year", (object?)year ?? DBNull.Value);
        TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());

        // Sayfalı veri çekimi


        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT Books.Id, Books.Title, Books.Author, Books.Year, Books.Publisher, Books.Language, Books.MD5, Books.Extension
            FROM Books
            JOIN BooksFTS ON Books.Id = BooksFTS.rowid
            WHERE 1=1
            {ftsClause}
            AND (@language IS NULL OR Books.Language = @language)
            AND (@year IS NULL OR Books.Year = @year)
            ORDER BY Books.Id DESC
            LIMIT @pageSize OFFSET @offset";

        cmd.Parameters.AddWithValue("@searchTitle", (object?)searchTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ftsQuery", string.IsNullOrEmpty(searchTitle) ? DBNull.Value : $"{searchTitle}*");
        cmd.Parameters.AddWithValue("@language", (object?)language ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@year", (object?)year ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pageSize", PageSize);
        cmd.Parameters.AddWithValue("@offset", (PageNumber - 1) * PageSize);


        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Books.Add(new BookDto
            {
                Id = reader.GetInt32(0),
                Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                Author = reader.IsDBNull(2) ? null : reader.GetString(2),
                Year = reader.IsDBNull(3) ? null : reader.GetString(3),
                Publisher = reader.IsDBNull(4) ? null : reader.GetString(4),
                Language = reader.IsDBNull(5) ? null : reader.GetString(5),
                MD5 = reader.IsDBNull(6) ? null : reader.GetString(6),
                Extension = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }
    }

    private List<string> QueryColumnDistinct(SqliteConnection conn, string column)
    {
        List<string> values = new();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT DISTINCT {column} FROM Books WHERE {column} IS NOT NULL AND TRIM({column}) != '' ORDER BY {column}";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            values.Add(reader.GetString(0));
        return values;
    }

    private string EscapeFtsQuery(string input)
    {
        return input.Replace("'", "''"); // SQL injection'dan korumak için
    }

    public async Task<IActionResult> OnPostScrapeAsync()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            _configuration["Database:BaseFolder"]!,
            _configuration["Database:FileName"]!
        );

        var config = _configuration.GetSection("LibgenConfig").Get<LibgenConfig>();
        var scraper = new LibgenScraper(dbPath, config, _hubContext);
        _ = Task.Run(() => scraper.Run()); // Run in background

        return RedirectToPage();
    }

  
}
