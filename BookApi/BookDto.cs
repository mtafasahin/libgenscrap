public class BookDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Series { get; set; }
    public string? Author { get; set; }
    public string? Year { get; set; }
    public string? Publisher { get; set; }
    public string? Language { get; set; }
    public string? Identifier { get; set; }
    public long FileSize { get; set; }
    public string? Extension { get; set; }
    public string? MD5 { get; set; }
    public bool IsNew { get; set; }
}
