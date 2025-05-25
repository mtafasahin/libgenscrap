using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.SignalR;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using BookApi;


public class LibgenScraper
{
    private readonly string _dbPath;
    private readonly LibgenConfig _config;
    private readonly IHubContext<ScrapingHub> _hubContext;

    public LibgenScraper(string dbPath, LibgenConfig config, IHubContext<ScrapingHub> hubContext)
    {
        _dbPath = dbPath;
        _config = config;
        _hubContext = hubContext;
    }

    public async Task Run()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MAX(Id) FROM Books";
        var result = cmd.ExecuteScalar();
        int prevMaxId = result != null ? Convert.ToInt32(result) : 1;
        int newMaxId = 0;
        int currentId = 100000000;
        int currentPageNumber = 0;

        using var driver = new ChromeDriver();
        List<BookRow> validRows = new();

        while (currentId >= prevMaxId)
        {
            try
            {
                currentPageNumber++;
                driver.Navigate().GoToUrl($"{_config.BaseUrl}&page={currentPageNumber}");
                Thread.Sleep(300);

                var rows = driver.FindElements(By.CssSelector("table.c > tbody > tr"));
                if (rows.Count == 0) break;

                foreach (var row in rows)
                {
                    try
                    {
                        var cells = row.FindElements(By.TagName("td"));
                        if (cells.Count < 7) continue;

                        string language = cells[6].Text.Trim();
                        string yearText = cells[4].Text.Trim();
                        string publisher = cells[3].Text.Trim();
                        string idStr = cells[0].Text.Trim();
                        if (!int.TryParse(idStr, out currentId)) continue;
                        if (currentId > newMaxId) newMaxId = currentId;

                        if(currentId < prevMaxId) break;

                        if (!int.TryParse(yearText, out int year)) continue;
                        bool validLang = _config.ValidLanguages.Contains(language) || _config.IgnoreLanguage;
                        bool validYear = (_config.ValidYearsFrom <= year && year <= _config.ValidYearsTo) || _config.IgnoreYear;

                        if (validLang && validYear)
                        {
                            await _hubContext.Clients.All.SendAsync("ScrapingProgress", $"Fetched: {currentId}, Target: {prevMaxId}");
                            string authors = cells[1].Text.Trim();
                            string title = cells[2].Text.Trim();
                            string extension = cells[8].Text.Trim();
                            string md5 = cells[11].Text.Trim();

                            using var insertCmd = connection.CreateCommand();
                            insertCmd.CommandText = "INSERT INTO Books (Id, Title, Author, Year, Publisher, Language, Extension, MD5) VALUES (@id, @title, @auth, @year, @pub, @lang, @ext, @md5)";
                            insertCmd.Parameters.AddWithValue("@id", currentId);
                            insertCmd.Parameters.AddWithValue("@title", title);
                            insertCmd.Parameters.AddWithValue("@auth", authors);
                            insertCmd.Parameters.AddWithValue("@year", yearText);
                            insertCmd.Parameters.AddWithValue("@pub", publisher);
                            insertCmd.Parameters.AddWithValue("@lang", language);
                            insertCmd.Parameters.AddWithValue("@ext", extension);
                            insertCmd.Parameters.AddWithValue("@md5", md5);
                            insertCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            await _hubContext.Clients.All.SendAsync("ScrapingProgress", $"Skipped: {currentId} - Language: {language}, Year: {yearText}, Target: {prevMaxId}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine("Row error: " + ex1.Message);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Page error: " + ex.Message);
                continue;
            }
        }

        driver.Quit();
        await _hubContext.Clients.All.SendAsync("ScrapingProgress", "✅ Scraping complete.");
    }
}
