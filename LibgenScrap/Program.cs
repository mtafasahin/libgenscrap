using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

class Program
{
    static void Main()
    {
        IWebDriver driver = new ChromeDriver();
        List<BookRow> validRows = new List<BookRow>();

        // Sayfalar arasında gezin
        for (int i = 729; i > 0; i--)
        {
            driver.Navigate().GoToUrl($"https://libgen.is/search.php?mode=last&view=simple&phrase=1&timefirst=&timelast=&sort=def&sortmode=ASC&page={i}");
            Thread.Sleep(300);

            var rows = driver.FindElements(By.CssSelector("table.c > tbody > tr"));

            foreach (var row in rows)
            {
                try
                {
                    var cells = row.FindElements(By.TagName("td"));

                    if (cells.Count < 7)
                        continue;

                    string language = cells[6].Text.Trim();
                    string yearText = cells[4].Text.Trim();
                    string publisher = cells[3].Text.Trim();

                    if (!int.TryParse(yearText, out int year))
                        continue;

                    bool validLanguage = language.Equals("English", StringComparison.OrdinalIgnoreCase) || 
                                         language.Equals("Turkish", StringComparison.OrdinalIgnoreCase);

                    bool validYear = year == 2023 || year == 2024 || year == 2025;

                    if (validLanguage && validYear)
                    {
                        string id = cells[0].Text.Trim();
                        string authors = cells[1].Text.Trim();
                        string title = cells[2].Text.Trim();
                        string titleLink = cells[2].FindElement(By.TagName("a")).GetAttribute("href");
                        string pages = cells[5].Text.Trim();
                        string size = cells[7].Text.Trim();
                        string extension = cells[8].Text.Trim();
                        string mirrors = cells[9].Text.Trim() + " " + cells[10].Text.Trim();
                        string mirrorLink1 = cells[9].FindElement(By.TagName("a")).GetAttribute("href");
                        string mirrorLink2 = cells[10].FindElement(By.TagName("a")).GetAttribute("href");

                        // Her şeyi sakla
                        validRows.Add(new BookRow
                        {
                            Id = id,
                            Authors = authors,
                            Title = title,
                            TitleLink = titleLink,
                            Publisher = publisher,
                            Year = yearText,
                            Pages = pages,
                            Language = language,
                            Size = size,
                            Extension = extension,
                            Mirrors = mirrors,
                            MirrorLink1 = mirrorLink1,
                            MirrorLink2 = mirrorLink2,
                            Html = row.GetAttribute("outerHTML")
                        });
                    }
                }
                catch { continue; }
            }
        }

        // Gruplama ve sıralama işlemi
        var groupedRows = validRows.OrderBy(r => r.Publisher).ToList();

        // Sayfa 1'e dön ve tabloyu temizle
        driver.Navigate().GoToUrl("https://libgen.is/search.php?mode=last&view=simple&phrase=1&timefirst=&timelast=&sort=def&sortmode=ASC&page=1");
        Thread.Sleep(500);

        var tableBody = driver.FindElement(By.CssSelector("table.c > tbody"));

        // Tüm mevcut <tr> elemanlarını sil
        foreach (var tr in tableBody.FindElements(By.TagName("tr")))
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].remove();", tr);
        }

        // 25'erli şekilde ekleme
        for (int i = 0; i < groupedRows.Count; i++)
        {
            if (i % 25 == 0)
            {
                Console.WriteLine($"Sayfa {(i / 25) + 1} gösteriliyor...");
                // Thread.Sleep(500);

                // Önce varsa eski <tr>'leri temizle
                var oldRows = tableBody.FindElements(By.TagName("tr"));
                foreach (var tr in oldRows)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].remove();", tr);
                }
            }

            string appendScript = "var tbody = arguments[0]; tbody.insertAdjacentHTML('beforeend', arguments[1]);";
            ((IJavaScriptExecutor)driver).ExecuteScript(appendScript, tableBody, groupedRows[i].Html);
        }

        Console.WriteLine("Tüm veriler gruplandı ve yeniden eklendi.");
        Console.ReadKey();
        driver.Quit();
    }
}
