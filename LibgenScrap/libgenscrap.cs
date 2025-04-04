using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;



class ProgramLibgenScrap
{
    static bool SeansBulVeSec(IWebDriver driver, string targetDate, 
                        TimeSpan startTime, TimeSpan endTime, 
                        List<string> allowedGenders)
    {

        var seansDivleri = driver.FindElements(By.CssSelector("#dvScheduler .panel-info"));
        var validBaslanicSaati = false;
        var validCinsyet = false;
        var validKontenjan = false;
        var validDate = false;

        foreach (var panel in seansDivleri)
        {
            try
            {
                // 1. Tarihi al
                var dateElement = panel.FindElement(By.CssSelector(".panel-heading .panel-title"));
                string panelDateText = dateElement.Text.Split("<br>").Last().Trim(); // "07.04.2025" olmalı

                validDate = panelDateText.Contains(targetDate);
                // if (!panelDateText.Contains(targetDate))
                //     continue;

                // 2. Seans kutularını tara
                var seansKutulari = panel.FindElements(By.CssSelector(".panel-body .well"));

                foreach (var seans in seansKutulari)
                {
                    try
                    {
                        // Saat
                        string saatAraligi = seans.FindElement(By.CssSelector("span[id*='lblSeansSaat']")).Text.Trim();
                        var saatler = saatAraligi.Split('-');
                        TimeSpan baslangic = TimeSpan.Parse(saatler[0].Trim());
                        TimeSpan bitis = TimeSpan.Parse(saatler[1].Trim());

                        validBaslanicSaati = baslangic >= startTime && baslangic <= endTime;
                        // if (baslangic < startTime || bitis > endTime)
                        //     continue;

                        // Cinsiyet
                        var cinsiyet = seans.FindElement(By.CssSelector("label.cinsiyet")).Text.Trim();
                        validCinsyet = allowedGenders.Contains(cinsiyet);
                        // if (!allowedGenders.Contains(cinsiyet))
                        //     continue;

                        // Kalan kontenjan
                        var kontenjanSpan = seans.FindElement(By.CssSelector("span[title='Kalan Kontenjan']"));
                        int kontenjan = int.Parse(kontenjanSpan.Text.Trim());
                        validKontenjan = kontenjan > 0;
                        // if (kontenjan <= 0)
                        //     continue;

                        if(!validBaslanicSaati || !validCinsyet || !validKontenjan || !validDate) {
                            Console.WriteLine($"{DateTime.Now} - Valid Bir Seans Değil : {panelDateText} {saatAraligi} - {cinsiyet} - Kontenjan: {kontenjan}");
                            continue;
                        }
                            
                        // Checkbox
                        var checkbox = seans.FindElements(By.CssSelector("input[type='checkbox']")).FirstOrDefault();
                        if (checkbox != null && checkbox.Enabled && !checkbox.Selected)
                        {
                            checkbox.Click();
                            Console.WriteLine($"Seans seçildi: {panelDateText} {saatAraligi} - {cinsiyet} - Kontenjan: {kontenjan}");
                            return true; // ilk bulduğunda çıkmak istersen
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine("Seans içi hata: " + innerEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Panel hata: " + ex.Message);
            }
        }

        Console.WriteLine("Uygun seans bulunamadı.");
        return false; // Seans bulunamadı
    }

    static void LibgenScrapMain()
    {
        IWebDriver driver = new ChromeDriver();
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        driver.Navigate().GoToUrl($"https://online.spor.istanbul/anasayfa");
        Thread.Sleep(2000);
        // Sayfanın tamamen yüklenmesini bekle (gerekirse)
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        // <a href="uyegiris.aspx"> etiketini href'e göre bul
        IWebElement uyeGirisLink = driver.FindElement(By.CssSelector("a[href='uyegiris.aspx']"));
        // Click işlemi yap
        uyeGirisLink.Click();

        // WebDriverWait tanımla
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        Thread.Sleep(3000);
        // TC/Pasaport numarasını gir
        var tcInput = driver.FindElement(By.Name("txtTCPasaport"));
        tcInput.Clear();
        tcInput.SendKeys("xxxxx"); // Buraya gerçek TC/Pasaport verisini yaz

        // Şifreyi gir
        var sifreInput = driver.FindElement(By.Name("txtSifre"));
        sifreInput.Clear();
        sifreInput.SendKeys("****"); // Şifreni buraya yaz
        Thread.Sleep(1000);
        // Giriş butonuna tıkla
        var girisButonu = driver.FindElement(By.Name("btnGirisYap"));
        girisButonu.Click();
        Thread.Sleep(3000);
        // "https://online.spor.istanbul/uyespor.aspx" // Giriş yaptıktan sonra yönlendirilmesi gereken URL
        driver.Navigate().GoToUrl("https://online.spor.istanbul/uyespor.aspx");
        Thread.Sleep(3000);

        // Giriş sonrası Seans Seç butonu yüklenene kadar bekle
        wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("pageContent_rptListe_lbtnSeansSecim_0")));
        // Butona tıkla
        driver.FindElement(By.Id("pageContent_rptListe_lbtnSeansSecim_0")).Click();
        Thread.Sleep(3000);

        bool seansBulundu = false;
        while (!seansBulundu)
        {
            // Seans bulma işlemi
            seansBulundu = SeansBulVeSec(
                driver,
                "08.04.2025",
                TimeSpan.Parse("05:00"),
                TimeSpan.Parse("09:00"),
                new List<string> { "Erkek", "Karma" }
            );

            if (!seansBulundu)
            {
                // Eğer seans bulunamazsa, 5 saniye bekle ve tekrar dene
                Thread.Sleep(5000);
                driver.Navigate().GoToUrl("https://online.spor.istanbul/uyeseanssecim");
                Thread.Sleep(3000);
            }
        }
        

        driver.Quit();
    }
}