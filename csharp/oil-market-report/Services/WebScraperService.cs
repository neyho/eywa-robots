using System;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OilMarketReport.Models;

namespace OilMarketReport.Services;

/// <summary>
/// Web scraping service using Selenium ChromeDriver
/// Collects oil prices and exchange rates from public sources
/// </summary>
public class WebScraperService : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly int _retryAttempts;
    private readonly int _retryDelaySeconds;
    
    public WebScraperService(bool headless = false, int retryAttempts = 3, int retryDelaySeconds = 5)
    {
        _retryAttempts = retryAttempts;
        _retryDelaySeconds = retryDelaySeconds;
        
        // Configure Chrome options
        var options = new ChromeOptions();
        
        if (headless)
        {
            options.AddArgument("--headless=new");
        }
        
        // Additional options for stability
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36");
        
        // Initialize driver
        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }
    
    /// <summary>
    /// Scrape Brent Crude oil price from Investing.com
    /// </summary>
    public async Task<OilPrice> ScrapeBrentPrice(string url = "https://www.investing.com/commodities/brent-oil")
    {
        return await ScrapeWithRetry(async () =>
        {
            Console.WriteLine($"[WebScraper] Navigating to Brent Crude: {url}");
            _driver.Navigate().GoToUrl(url);
            
            // Wait for price element to load
            await Task.Delay(2000); // Give page time to fully load
            
            var oilPrice = new OilPrice
            {
                Product = "Brent",
                SourceUrl = url,
                Timestamp = DateTime.Now
            };
            
            try
            {
                // Current price - try multiple selectors
                var priceElement = FindElement(
                    By.CssSelector("[data-test='instrument-price-last']"),
                    By.CssSelector(".text-2xl"),
                    By.XPath("//span[contains(@class, 'text-2xl')]")
                );
                
                if (priceElement != null)
                {
                    var priceText = priceElement.Text.Replace(",", "").Replace("$", "").Trim();
                    oilPrice.CurrentPrice = decimal.Parse(priceText);
                    Console.WriteLine($"[WebScraper] Brent current price: ${oilPrice.CurrentPrice}");
                }
                
                // Daily change
                var changeElement = FindElement(
                    By.CssSelector("[data-test='instrument-price-change']"),
                    By.XPath("//span[contains(@data-test, 'change')]")
                );
                
                if (changeElement != null)
                {
                    var changeText = changeElement.Text.Replace("+", "").Replace("$", "").Trim();
                    oilPrice.DailyChange = decimal.Parse(changeText);
                }
                
                // Daily change percent
                var changePercentElement = FindElement(
                    By.CssSelector("[data-test='instrument-price-change-percent']"),
                    By.XPath("//span[contains(@data-test, 'change-percent')]")
                );
                
                if (changePercentElement != null)
                {
                    var percentText = changePercentElement.Text
                        .Replace("+", "")
                        .Replace("-", "")
                        .Replace("%", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Trim();
                    
                    oilPrice.DailyChangePercent = decimal.Parse(percentText);
                    
                    // Restore sign from the element
                    if (changePercentElement.Text.Contains("-"))
                    {
                        oilPrice.DailyChange = -Math.Abs(oilPrice.DailyChange);
                        oilPrice.DailyChangePercent = -Math.Abs(oilPrice.DailyChangePercent);
                    }
                }
                
                // Calculate previous close
                oilPrice.PreviousClose = oilPrice.CurrentPrice - oilPrice.DailyChange;
                
                // Stub for 7-day average (will be calculated from historical data later)
                oilPrice.SevenDayAverage = oilPrice.CurrentPrice;
                
                Console.WriteLine($"[WebScraper] Brent scraped successfully: ${oilPrice.CurrentPrice} ({oilPrice.DailyChangePercent:+0.00;-0.00}%)");
                
                return oilPrice;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebScraper] Error parsing Brent data: {ex.Message}");
                TakeScreenshot("brent_error");
                throw;
            }
        });
    }
    
    /// <summary>
    /// Scrape WTI Crude oil price from Investing.com
    /// </summary>
    public async Task<OilPrice> ScrapeWTIPrice(string url = "https://www.investing.com/commodities/crude-oil")
    {
        return await ScrapeWithRetry(async () =>
        {
            Console.WriteLine($"[WebScraper] Navigating to WTI Crude: {url}");
            _driver.Navigate().GoToUrl(url);
            
            await Task.Delay(2000);
            
            var oilPrice = new OilPrice
            {
                Product = "WTI",
                SourceUrl = url,
                Timestamp = DateTime.Now
            };
            
            try
            {
                // Same selectors as Brent (same site structure)
                var priceElement = FindElement(
                    By.CssSelector("[data-test='instrument-price-last']"),
                    By.CssSelector(".text-2xl")
                );
                
                if (priceElement != null)
                {
                    var priceText = priceElement.Text.Replace(",", "").Replace("$", "").Trim();
                    oilPrice.CurrentPrice = decimal.Parse(priceText);
                }
                
                var changeElement = FindElement(By.CssSelector("[data-test='instrument-price-change']"));
                if (changeElement != null)
                {
                    var changeText = changeElement.Text.Replace("+", "").Replace("$", "").Trim();
                    oilPrice.DailyChange = decimal.Parse(changeText);
                }
                
                var changePercentElement = FindElement(By.CssSelector("[data-test='instrument-price-change-percent']"));
                if (changePercentElement != null)
                {
                    var percentText = changePercentElement.Text
                        .Replace("+", "").Replace("-", "").Replace("%", "")
                        .Replace("(", "").Replace(")", "").Trim();
                    
                    oilPrice.DailyChangePercent = decimal.Parse(percentText);
                    
                    if (changePercentElement.Text.Contains("-"))
                    {
                        oilPrice.DailyChange = -Math.Abs(oilPrice.DailyChange);
                        oilPrice.DailyChangePercent = -Math.Abs(oilPrice.DailyChangePercent);
                    }
                }
                
                oilPrice.PreviousClose = oilPrice.CurrentPrice - oilPrice.DailyChange;
                oilPrice.SevenDayAverage = oilPrice.CurrentPrice;
                
                Console.WriteLine($"[WebScraper] WTI scraped successfully: ${oilPrice.CurrentPrice} ({oilPrice.DailyChangePercent:+0.00;-0.00}%)");
                
                return oilPrice;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebScraper] Error parsing WTI data: {ex.Message}");
                TakeScreenshot("wti_error");
                throw;
            }
        });
    }
    
    /// <summary>
    /// Scrape EUR/USD exchange rate from Investing.com
    /// </summary>
    public async Task<ExchangeRate> ScrapeEurUsd(string url = "https://www.investing.com/currencies/eur-usd")
    {
        return await ScrapeWithRetry(async () =>
        {
            Console.WriteLine($"[WebScraper] Navigating to EUR/USD: {url}");
            _driver.Navigate().GoToUrl(url);
            
            await Task.Delay(2000);
            
            var rate = new ExchangeRate
            {
                Pair = "EUR/USD",
                SourceUrl = url,
                Timestamp = DateTime.Now
            };
            
            try
            {
                var priceElement = FindElement(
                    By.CssSelector("[data-test='instrument-price-last']"),
                    By.CssSelector(".text-2xl")
                );
                
                if (priceElement != null)
                {
                    var rateText = priceElement.Text.Replace(",", "").Trim();
                    rate.CurrentRate = decimal.Parse(rateText);
                }
                
                var changeElement = FindElement(By.CssSelector("[data-test='instrument-price-change']"));
                if (changeElement != null)
                {
                    var changeText = changeElement.Text.Replace("+", "").Trim();
                    rate.DailyChange = decimal.Parse(changeText);
                }
                
                var changePercentElement = FindElement(By.CssSelector("[data-test='instrument-price-change-percent']"));
                if (changePercentElement != null)
                {
                    var percentText = changePercentElement.Text
                        .Replace("+", "").Replace("-", "").Replace("%", "")
                        .Replace("(", "").Replace(")", "").Trim();
                    
                    rate.DailyChangePercent = decimal.Parse(percentText);
                    
                    if (changePercentElement.Text.Contains("-"))
                    {
                        rate.DailyChange = -Math.Abs(rate.DailyChange);
                        rate.DailyChangePercent = -Math.Abs(rate.DailyChangePercent);
                    }
                }
                
                rate.PreviousRate = rate.CurrentRate - rate.DailyChange;
                rate.SevenDayAverage = rate.CurrentRate;
                
                Console.WriteLine($"[WebScraper] EUR/USD scraped successfully: {rate.CurrentRate:F4} ({rate.DailyChangePercent:+0.00;-0.00}%)");
                
                return rate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebScraper] Error parsing EUR/USD data: {ex.Message}");
                TakeScreenshot("eurusd_error");
                throw;
            }
        });
    }
    
    /// <summary>
    /// Scrape USD/RSD exchange rate from National Bank of Serbia
    /// </summary>
    public async Task<ExchangeRate> ScrapeUsdRsd(string url = "https://www.nbs.rs/kursnaListaModul/srednjiKurs.faces")
    {
        return await ScrapeWithRetry(async () =>
        {
            Console.WriteLine($"[WebScraper] Navigating to NBS for USD/RSD: {url}");
            _driver.Navigate().GoToUrl(url);
            
            await Task.Delay(3000); // NBS site may be slower
            
            var rate = new ExchangeRate
            {
                Pair = "USD/RSD",
                SourceUrl = url,
                Timestamp = DateTime.Now
            };
            
            try
            {
                // Look for USD in the exchange rate table
                // NBS shows rates in format: USD | 1 | 109.5000
                var rows = _driver.FindElements(By.CssSelector("table tr"));
                
                foreach (var row in rows)
                {
                    var cells = row.FindElements(By.TagName("td"));
                    if (cells.Count >= 3)
                    {
                        var currency = cells[0].Text.Trim();
                        if (currency.Contains("USD") || currency.Contains("840"))
                        {
                            // Middle rate is typically in the 3rd or 4th column
                            var rateText = cells[2].Text.Replace(",", ".").Trim();
                            rate.CurrentRate = decimal.Parse(rateText);
                            
                            Console.WriteLine($"[WebScraper] Found USD rate: {rate.CurrentRate:F2} RSD");
                            break;
                        }
                    }
                }
                
                if (rate.CurrentRate == 0)
                {
                    throw new Exception("USD/RSD rate not found in NBS table");
                }
                
                // NBS doesn't show daily change, so we'll calculate it later if we have historical data
                // For now, stub with small change
                rate.DailyChange = 0;
                rate.DailyChangePercent = 0;
                rate.PreviousRate = rate.CurrentRate;
                rate.SevenDayAverage = rate.CurrentRate;
                
                Console.WriteLine($"[WebScraper] USD/RSD scraped successfully: {rate.CurrentRate:F2}");
                
                return rate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebScraper] Error parsing USD/RSD data: {ex.Message}");
                TakeScreenshot("usdrsd_error");
                throw;
            }
        });
    }
    
    /// <summary>
    /// Retry wrapper for scraping operations
    /// </summary>
    private async Task<T> ScrapeWithRetry<T>(Func<Task<T>> scrapeFunc)
    {
        Exception? lastException = null;
        
        for (int attempt = 1; attempt <= _retryAttempts; attempt++)
        {
            try
            {
                return await scrapeFunc();
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"[WebScraper] Attempt {attempt}/{_retryAttempts} failed: {ex.Message}");
                
                if (attempt < _retryAttempts)
                {
                    var delay = _retryDelaySeconds * attempt; // Exponential backoff
                    Console.WriteLine($"[WebScraper] Retrying in {delay} seconds...");
                    await Task.Delay(delay * 1000);
                }
            }
        }
        
        throw new Exception($"Failed after {_retryAttempts} attempts", lastException);
    }
    
    /// <summary>
    /// Find element using multiple selectors (fallback strategy)
    /// </summary>
    private IWebElement? FindElement(params By[] selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var element = _driver.FindElement(selector);
                if (element != null && !string.IsNullOrEmpty(element.Text))
                {
                    return element;
                }
            }
            catch (NoSuchElementException)
            {
                // Try next selector
                continue;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Take screenshot for debugging
    /// </summary>
    private void TakeScreenshot(string filename)
    {
        try
        {
            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
            var path = $"/tmp/{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            screenshot.SaveAsFile(path);
            Console.WriteLine($"[WebScraper] Screenshot saved: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebScraper] Failed to take screenshot: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Dispose ChromeDriver
    /// </summary>
    public void Dispose()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebScraper] Error disposing driver: {ex.Message}");
        }
    }
}