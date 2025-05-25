using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


public class CoverModel : PageModel
{
    public async Task<IActionResult> OnGetAsync(string id, string md5)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(md5))
            return BadRequest();

        var folder = (int.Parse(id) / 1000) * 1000;
        var imageUrl = $"https://libgen.is/covers/{folder:D7}/{md5.ToLowerInvariant()}-g.jpg";

        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var stream = await response.Content.ReadAsStreamAsync();
            return File(stream, contentType);
        }
        catch
        {
            return NotFound();
        }
    }
}

