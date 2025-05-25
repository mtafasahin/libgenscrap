using Microsoft.Data.Sqlite;

public class BookRepository : IBookRepository
{
    private readonly string _connectionString;

    public BookRepository(IConfiguration config)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseFolder = config["Database:BaseFolder"];
        var fileName = config["Database:FileName"];

        var fullPath = Path.Combine(localAppData, baseFolder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        _connectionString = $"Data Source={fullPath}";
    }

    public async Task<PagedResult<BookDto>> GetBooksAsync(BookSearchRequest request)
    {
        var result = new PagedResult<BookDto>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var items = new List<BookDto>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var filters = new List<string>();
        var cmd = conn.CreateCommand();

        if (!string.IsNullOrWhiteSpace(request.Language))
            filters.Add("Language = @language");
        if (!string.IsNullOrWhiteSpace(request.Title))
            filters.Add("Title LIKE @title");
        if (!string.IsNullOrWhiteSpace(request.Author))
            filters.Add("Author LIKE @author");
        if (!string.IsNullOrWhiteSpace(request.Publisher))
            filters.Add("Publisher LIKE @publisher");
        if (!string.IsNullOrWhiteSpace(request.Year))
            filters.Add("Year = @year");

        string whereClause = filters.Any() ? " WHERE " + string.Join(" AND ", filters) : "";

        // Count
        var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM Books{whereClause}";
        AddParameters(countCmd, request);
        result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        // Data query
        cmd.CommandText = $@"
            SELECT Id, Title, Series, Author, Year, Publisher, Language, Identifier, FileSize, Extension, MD5
            FROM Books
            {whereClause}
            LIMIT @limit OFFSET @offset";

        AddParameters(cmd, request);
        cmd.Parameters.AddWithValue("@limit", request.PageSize);
        cmd.Parameters.AddWithValue("@offset", (request.PageNumber - 1) * request.PageSize);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dto = new BookDto
            {
                Id = reader.GetInt32(0),
                Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                Series = reader.IsDBNull(2) ? null : reader.GetString(2),
                Author = reader.IsDBNull(3) ? null : reader.GetString(3),
                Year = reader.IsDBNull(4) ? null : reader.GetString(4),
                Publisher = reader.IsDBNull(5) ? null : reader.GetString(5),
                Language = reader.IsDBNull(6) ? null : reader.GetString(6),
                Identifier = reader.IsDBNull(7) ? null : reader.GetString(7),
                FileSize = reader.GetInt64(8),
                Extension = reader.IsDBNull(9) ? null : reader.GetString(9),
                MD5 = reader.IsDBNull(10) ? null : reader.GetString(10)
            };
            items.Add(dto);
        }

        result.Items = items;
        return result;
    }


private static void AddParameters(SqliteCommand cmd, BookSearchRequest request)
{
    if (!string.IsNullOrWhiteSpace(request.Language))
        cmd.Parameters.AddWithValue("@language", request.Language);
    if (!string.IsNullOrWhiteSpace(request.Title))
        cmd.Parameters.AddWithValue("@title", $"%{request.Title}%");
    if (!string.IsNullOrWhiteSpace(request.Author))
        cmd.Parameters.AddWithValue("@author", $"%{request.Author}%");
    if (!string.IsNullOrWhiteSpace(request.Publisher))
        cmd.Parameters.AddWithValue("@publisher", $"%{request.Publisher}%");
    if (!string.IsNullOrWhiteSpace(request.Year))
        cmd.Parameters.AddWithValue("@year", request.Year);
}

}
