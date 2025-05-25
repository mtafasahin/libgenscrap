using System;

namespace BookApi;

class BookRow
{
    public string Id { get; set; }
    public string Authors { get; set; }
    public string Title { get; set; }
    public string TitleLink { get; set; }  // Title'ın Linki
    public string Publisher { get; set; }
    public string Year { get; set; }
    public string Pages { get; set; }
    public string Language { get; set; }
    public string Size { get; set; }
    public string Extension { get; set; }
    public string Mirrors { get; set; }
    public string MirrorLink1 { get; set; }
    public string MirrorLink2 { get; set; }
    public string Html { get; set; }  // outerHTML of the <tr>
}
