using System;

namespace BookApi;

public class LibgenConfig
{
    public string LastMaxIdFileName { get; set; } = "lastmaxid.txt";
    public string AppDirecory { get; set; } = "libgenscrap";
    public string BaseUrl { get; set; } = "https://libgen.is/search.php?mode=last&view=simple&phrase=1&timefirst=&timelast=&sort=def&sortmode=ASC&page=";
    public List<string> ValidLanguages { get; set; } = new List<string> { "Turkish" };
    public int ValidYearsFrom { get; set; } = 2017;
    public int ValidYearsTo { get; set; } = DateTime.Now.Year;
    public bool Override { get; set; }
    public int MaxId { get; set; }

    public bool IgnoreYear { get; set; }
    public bool IgnoreLanguage { get; set; }

    public string DbName { get; set; } = "libgen.db";
}