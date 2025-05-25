using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;


public class DownloadModel : PageModel
{
    public async Task<IActionResult> OnGetAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("URL gerekli.");

        using var httpClient = new HttpClient();

        try
        {
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return NotFound("Dosya bulunamadı.");

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            var stream = await response.Content.ReadAsStreamAsync();

            // 🧠 Kitap ID'sini URL'den çıkar
            var uri = new Uri(url);
            var segments = uri.Segments;
            var folder = segments[^3].TrimEnd('/');
            if (int.TryParse(folder, out int folderStart))
            {
                // örn: 4499000 → 4499842 olmalı (url'den daha iyi bir çözüm varsa onunla değiştiririz)
                var possibleIdSegment = segments[^2]; // md5
                var bookId = FindBookIdByMD5(possibleIdSegment.TrimEnd('/'));

                if (bookId.HasValue)
                {
                    UpdateBookAsDownloaded(bookId.Value);
                }
            }

            return File(stream, contentType, fileName);
        }
        catch
        {
            return StatusCode(500, "Dosya indirilemedi.");
        }
    }

    private int? FindBookIdByMD5(string md5)
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "libgenscrap", "libgen.db");
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Books WHERE lower(MD5) = @md5 LIMIT 1";
        cmd.Parameters.AddWithValue("@md5", md5.ToLowerInvariant());

        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : null;
    }

    private void UpdateBookAsDownloaded(int bookId)
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "libgenscrap", "libgen.db");
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Books SET IsDownloaded = 1 WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", bookId);

        cmd.ExecuteNonQuery();
    }
}
