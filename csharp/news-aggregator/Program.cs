using System.Text.Json.Nodes;
using EywaClient;
using EywaClient.Core;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace NewsAggregator;

public class NewsArticle
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; }
    public int Priority { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int EngagementScore { get; set; }
}

public static class HackerNewsScraper
{
    public static async Task<List<NewsArticle>> ScrapeAsync(IWebDriver driver, int maxArticles = 8)
    {
        var articles = new List<NewsArticle>();
        
        try
        {
            Console.WriteLine("🔍 Scraping Hacker News...");
            
            driver.Navigate().GoToUrl("https://news.ycombinator.com");
            
            // Wait for page to load
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElements(By.ClassName("titleline")).Count > 0);

            var titleElements = driver.FindElements(By.ClassName("titleline"));
            var scoreElements = driver.FindElements(By.ClassName("score"));

            Console.WriteLine($"📊 Found {titleElements.Count} articles on Hacker News");

            for (int i = 0; i < Math.Min(maxArticles, titleElements.Count); i++)
            {
                try
                {
                    var titleElement = titleElements[i].FindElement(By.TagName("a"));
                    var title = titleElement.Text.Trim();
                    var url = titleElement.GetAttribute("href");

                    // Get engagement score if available
                    int engagementScore = 0;
                    if (i < scoreElements.Count)
                    {
                        var scoreText = scoreElements[i].Text;
                        var match = Regex.Match(scoreText, @"(\d+)");
                        if (match.Success)
                        {
                            int.TryParse(match.Groups[1].Value, out engagementScore);
                        }
                    }

                    var article = new NewsArticle
                    {
                        Id = $"hn_{i}_{DateTime.UtcNow.Ticks}",
                        Title = title,
                        Url = url ?? "https://news.ycombinator.com",
                        Source = "Hacker News",
                        Category = CategorizeTitle(title),
                        ScrapedAt = DateTime.UtcNow,
                        Priority = CalculatePriority(title, engagementScore),
                        Summary = $"Popular on HN with {engagementScore} points",
                        EngagementScore = engagementScore
                    };

                    articles.Add(article);
                    Console.WriteLine($"📰 Scraped: {title} ({engagementScore} points)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to scrape article {i}: {ex.Message}");
                }
            }

            // Be respectful - small delay
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error scraping Hacker News: {ex.Message}");
        }

        return articles;
    }

    private static string CategorizeTitle(string title)
    {
        var titleLower = title.ToLower();
        
        if (titleLower.Contains("ai") || titleLower.Contains("machine learning") || 
            titleLower.Contains("gpt") || titleLower.Contains("chatgpt"))
            return "AI & ML";
        
        if (titleLower.Contains("startup") || titleLower.Contains("funding") || 
            titleLower.Contains("ipo") || titleLower.Contains("venture"))
            return "Startups";
        
        if (titleLower.Contains("programming") || titleLower.Contains("code") || 
            titleLower.Contains("developer") || titleLower.Contains("github"))
            return "Programming";
        
        if (titleLower.Contains("crypto") || titleLower.Contains("bitcoin") || 
            titleLower.Contains("blockchain"))
            return "Crypto";
        
        if (titleLower.Contains("security") || titleLower.Contains("hack") || 
            titleLower.Contains("breach") || titleLower.Contains("vulnerability"))
            return "Security";
        
        return "Technology";
    }

    private static int CalculatePriority(string title, int engagementScore)
    {
        int priority = 1;
        
        // High engagement = higher priority
        if (engagementScore > 500) priority += 2;
        else if (engagementScore > 100) priority += 1;
        
        // Urgent keywords
        var titleLower = title.ToLower();
        if (titleLower.Contains("breaking") || titleLower.Contains("urgent") || 
            titleLower.Contains("critical") || titleLower.Contains("emergency"))
            priority += 2;
        
        // AI/ML gets priority (hot topic)
        if (titleLower.Contains("ai") || titleLower.Contains("gpt") || 
            titleLower.Contains("machine learning"))
            priority += 1;
        
        return Math.Min(priority, 5); // Cap at 5
    }
}

public static class WebDriverManager
{
    public static IWebDriver CreateChromeDriver(bool headless = true)
    {
        var options = new ChromeOptions();
        
        if (headless)
        {
            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
        }
        
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        
        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        
        return driver;
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var eywa = new Eywa();
        
        try
        {
            Console.WriteLine("🚀 EYWA News Aggregation Robot - Starting...");

            // Initialize EYWA communication
            eywa.OpenPipe();

            // Give pipe time to initialize
            await Task.Delay(100);

            // Get task from EYWA
            var task = await eywa.Tasks.GetTaskAsync();

            // Parse task parameters - GetTaskAsync returns JsonNode in 0.2.3+
            var maxArticles = task?["data"]?["maxArticlesPerSource"]?.GetValue<int>() ?? 8;
            var headless = task?["data"]?["headless"]?.GetValue<bool>() ?? true;
            var testMode = task?["data"]?["testMode"]?.GetValue<bool>() ?? false;

            await eywa.Logger.InfoAsync($"📋 Task started - maxArticles: {maxArticles}, headless: {headless}, testMode: {testMode}");
            await eywa.Tasks.UpdateTaskAsync(Status.Processing);
            
            Console.WriteLine("📰 EYWA News Aggregation Robot - SELENIUM EDITION! 🚀");
            Console.WriteLine("=========================================================");
            
            IWebDriver? driver = null;
            var allArticles = new List<NewsArticle>();
            
            try
            {
                // Initialize Chrome WebDriver
                await eywa.Logger.InfoAsync("🚀 Initializing Chrome WebDriver...");
                driver = WebDriverManager.CreateChromeDriver(headless);
                await eywa.Logger.InfoAsync("✅ Chrome WebDriver initialized successfully");

                // Scrape Hacker News
                await eywa.Logger.InfoAsync("🔄 Starting Hacker News scraping...");
                var articles = await HackerNewsScraper.ScrapeAsync(driver, maxArticles);
                allArticles.AddRange(articles);

                await eywa.Logger.InfoAsync($"✅ Hacker News: Found {articles.Count} articles");
                
                // Sort by priority and engagement
                var sortedArticles = allArticles
                    .OrderByDescending(a => a.Priority)
                    .ThenByDescending(a => a.EngagementScore)
                    .ThenByDescending(a => a.ScrapedAt)
                    .ToList();

                // Show results
                ShowNewsDigest(sortedArticles.Take(15).ToList());

                // TODO: Report results to EYWA when C# EywaClient supports Report method
                // var reportData = new Dictionary<string, object>
                // {
                //     ["articlesFound"] = allArticles.Count,
                //     ["sources"] = new[] { "Hacker News" },
                //     ["highPriority"] = allArticles.Count(a => a.Priority >= 4),
                //     ["categories"] = allArticles.Select(a => a.Category).Distinct().ToList(),
                //     ["topArticles"] = sortedArticles.Take(5).Select(a => new {
                //         title = a.Title,
                //         source = a.Source,
                //         priority = a.Priority,
                //         engagementScore = a.EngagementScore
                //     }).ToList()
                // };
                // eywa.Report("News Aggregation Complete", reportData);
                await eywa.Logger.InfoAsync($"✨ News aggregation complete! Processed {allArticles.Count} articles");
                await eywa.Tasks.CloseTaskAsync(Status.Success);
            }
            catch (Exception ex)
            {
                await eywa.Logger.ErrorAsync($"❌ Error during news aggregation: {ex.Message}");
                await eywa.Tasks.CloseTaskAsync(Status.Error);
            }
            finally
            {
                // Clean up WebDriver
                try
                {
                    driver?.Quit();
                    driver?.Dispose();
                    await eywa.Logger.InfoAsync("🔌 WebDriver disposed");
                }
                catch (Exception ex)
                {
                    await eywa.Logger.InfoAsync($"⚠️ Error disposing WebDriver: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await eywa.Logger.ErrorAsync($"❌ Fatal error: {ex.Message}");
            await eywa.Tasks.CloseTaskAsync(Status.Error);
        }
    }

    private static void ShowNewsDigest(List<NewsArticle> articles)
    {
        Console.WriteLine("\n📊 YOUR PERSONALIZED NEWS DIGEST");
        Console.WriteLine("".PadRight(80, '='));
        
        var categories = articles.GroupBy(a => a.Category).OrderByDescending(g => g.Count());
        
        foreach (var category in categories)
        {
            Console.WriteLine($"\n📁 {category.Key.ToUpper()} ({category.Count()} articles)");
            Console.WriteLine("".PadRight(50, '-'));
            
            foreach (var article in category.Take(5))
            {
                string priorityIcon = article.Priority switch
                {
                    5 => "🔥",
                    4 => "⚡",
                    3 => "📈",
                    _ => "📰"
                };
                
                Console.WriteLine($"{priorityIcon} {article.Title}");
                Console.WriteLine($"   🏠 {article.Source} | 🔗 {TruncateUrl(article.Url)}");
                if (article.EngagementScore > 0)
                    Console.WriteLine($"   👍 {article.EngagementScore} points");
                Console.WriteLine();
            }
        }

        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine($"📈 Total articles: {articles.Count}");
        Console.WriteLine($"📁 Categories: {string.Join(", ", categories.Select(c => c.Key))}");
        Console.WriteLine($"🔥 High priority: {articles.Count(a => a.Priority >= 4)}");
        Console.WriteLine($"🏠 Sources: Hacker News");
    }

    private static string TruncateUrl(string url)
    {
        if (url.Length <= 60) return url;
        return url.Substring(0, 57) + "...";
    }
}
