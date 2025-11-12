# 📰 EYWA News Aggregation Robot (C# + Selenium)

**REAL WEB SCRAPING WITH SELENIUM** - No APIs, no keys, just raw web automation! 🔥

A intelligent news aggregation robot that scrapes multiple news portals using Selenium WebDriver, categorizes articles automatically, and creates personalized news digests.

## 🎯 What This Robot ACTUALLY Does

- **🌐 Scrapes 3 Major News Portals**:
  - 📊 **Hacker News** (with real upvote scores!)
  - 📱 **TechCrunch** (latest tech news)
  - 💬 **Reddit r/technology** (trending discussions)

- **🤖 Smart Article Processing**:
  - ✨ **Categorizes articles** (AI, Startups, Security, Mobile, etc.)
  - ⚡ **Prioritizes by engagement** (upvotes, trending signals)
  - 🏷️ **Filters duplicates** and low-quality content
  - 📊 **Generates digest** grouped by category

- **🚀 Production Features**:
  - 🕒 **Respectful scraping** (delays between sites)
  - 🔄 **Robust error handling** (continues if one site fails)
  - 💾 **Stores in EYWA** for analysis and trending
  - 📱 **Headless browser** operation (runs in background)

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 or later
- **Google Chrome browser** installed
- ChromeDriver (auto-downloaded with package)
- Internet connection
- EYWA CLI (optional)

### Installation & Run

```bash
# Navigate to project
cd /Users/robi/dev/eywa-robots/csharp/news-aggregator

# Build and run
dotnet build
dotnet run

# Or with EYWA integration
eywa run -c "dotnet run"
```

## 📊 Expected Output

```
🚀 INITIALIZING NEWS AGGREGATION ROBOT...

✅ Connected to EYWA successfully!

📰 EYWA News Aggregation Robot - SELENIUM EDITION! 🚀
=========================================================

🚀 Initializing Chrome WebDriver (headless: True)
✅ Chrome WebDriver initialized successfully

🔄 Starting scraper: Hacker News
🔍 Scraping Hacker News...
📊 Found 30 articles on Hacker News
✅ Hacker News: Found 8 articles

🔄 Starting scraper: TechCrunch
🔍 Scraping TechCrunch...
📊 Found 25 potential articles on TechCrunch
✅ TechCrunch: Found 8 articles

🔄 Starting scraper: Reddit (r/technology)
🔍 Scraping Reddit r/technology...
📊 Found 20 posts on Reddit
✅ Reddit (r/technology): Found 7 articles

📊 YOUR PERSONALIZED NEWS DIGEST
================================================================================

📁 AI & ML (4 articles)
--------------------------------------------------
🔥 OpenAI announces GPT-5 with unprecedented capabilities
   🏠 Hacker News | 🔗 https://openai.com/blog/gpt-5-announcement
   👍 1247 points

⚡ Meta's new AI model beats GPT-4 on coding benchmarks
   🏠 TechCrunch | 🔗 https://techcrunch.com/2024/11/10/meta-ai-coding...

📈 Show HN: I built an AI that writes better code than me
   🏠 Hacker News | 🔗 https://news.ycombinator.com/item?id=12345
   👍 892 points

📁 STARTUPS (3 articles)
--------------------------------------------------
⚡ Y Combinator's latest batch raises $2B total funding
   🏠 TechCrunch | 🔗 https://techcrunch.com/2024/11/10/yc-funding...

📈 Show HN: Our startup hit $1M ARR in 8 months
   🏠 Hacker News | 🔗 https://news.ycombinator.com/item?id=67890
   👍 567 points

📁 PROGRAMMING (3 articles)
--------------------------------------------------
🔥 The state of JavaScript frameworks in 2024
   🏠 Hacker News | 🔗 https://2024.stateofjs.com/
   👍 1156 points

📈 GitHub Copilot now writes entire applications
   🏠 TechCrunch | 🔗 https://techcrunch.com/2024/11/10/copilot-apps...

📁 TECH DISCUSSION (2 articles)
--------------------------------------------------
⚡ Why are tech layoffs still happening in 2024?
   🏠 Reddit (r/technology) | 🔗 https://reddit.com/r/technology/comments...

📰 Apple's new MacBook Pro M4 review megathread
   🏠 Reddit (r/technology) | 🔗 https://reddit.com/r/technology/comments...

================================================================================
📈 Total articles: 23
📁 Categories: AI & ML, Startups, Programming, Security, Tech Discussion
🔥 High priority: 6
🏠 Sources: Hacker News, TechCrunch, Reddit (r/technology)

✨ News aggregation complete! Processed 23 articles
🔌 WebDriver disposed
```

## 🔧 **The REAL Selenium Implementation**

### **1. WebDriver Setup with Chrome**
```csharp
var options = new ChromeOptions();
options.AddArgument("--headless");                    // Background mode
options.AddArgument("--no-sandbox");                  // Security for containers  
options.AddArgument("--user-agent=Mozilla/5.0...");   // Avoid bot detection

_driver = new ChromeDriver(options);
_driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
```

### **2. Smart Element Selection**
```csharp
// Multiple selectors for robustness - sites change their HTML!
var articleSelectors = new[]
{
    "h2 a[href*='/2024/']",           // Main articles with year  
    "h3 a[href*='/2024/']",           // Secondary articles
    ".post-block__title__link",        // Alternative selector
    "a[data-module='ArticleTransitionIn']" // React component
};

foreach (var selector in articleSelectors)
{
    var elements = driver.FindElements(By.CssSelector(selector));
    allLinks.AddRange(elements);
    if (allLinks.Count >= maxArticles) break; // Got enough!
}
```

### **3. Engagement Score Extraction**
```csharp
// Extract Hacker News upvote scores with regex
var scoreElements = driver.FindElements(By.ClassName("score"));
var scoreText = scoreElements[i].Text; // "247 points"
var match = Regex.Match(scoreText, @"(\d+)");
if (match.Success)
{
    int.TryParse(match.Groups[1].Value, out engagementScore);
}
```

### **4. Respectful Scraping**
```csharp
// Be nice to websites - don't hammer them
await Task.Delay(2000, stoppingToken); // 2 second delay between sites

// Set realistic timeouts
_driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
_driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
```

### **5. Robust Error Handling**
```csharp
try
{
    var articles = await scraper.ScrapeNewsAsync(driver, maxArticles: 8);
    allArticles.AddRange(articles);
}
catch (Exception ex)
{
    _logger.LogError(ex, "❌ Error scraping {Portal}", scraper.PortalName);
    // Continue with other sites even if one fails!
}
```

## 🎯 **Intelligent Categorization**

The robot automatically categorizes articles using smart keyword detection:

```csharp
private string CategorizeTitle(string title)
{
    var titleLower = title.ToLower();
    
    // AI & ML Detection
    if (titleLower.Contains("ai") || titleLower.Contains("gpt") || 
        titleLower.Contains("machine learning"))
        return "AI & ML";
    
    // Startup News
    if (titleLower.Contains("startup") || titleLower.Contains("funding") || 
        titleLower.Contains("ipo"))
        return "Startups";
    
    // Security Issues  
    if (titleLower.Contains("security") || titleLower.Contains("breach") || 
        titleLower.Contains("vulnerability"))
        return "Security";
        
    return "Technology"; // Default category
}
```

## ⚡ **Priority Algorithm**

Articles get priority scores based on multiple factors:

```csharp
private int CalculatePriority(string title, int engagementScore)
{
    int priority = 1;
    
    // High engagement = higher priority
    if (engagementScore > 500) priority += 2;      // 🔥 Viral content
    else if (engagementScore > 100) priority += 1;  // ⚡ Popular content
    
    // Urgent keywords
    if (titleLower.Contains("breaking") || titleLower.Contains("critical"))
        priority += 2;  // 🚨 Breaking news
    
    // Hot topics get boost
    if (titleLower.Contains("ai") || titleLower.Contains("gpt"))
        priority += 1;  // 📈 Trending topics
        
    return Math.Min(priority, 5); // Cap at maximum priority
}
```

## 🛡️ **Anti-Bot Detection**

The robot includes several techniques to avoid being blocked:

```csharp
// Realistic user agent
options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

// Random delays between actions
await Task.Delay(Random.Shared.Next(1000, 3000));

// Respect robots.txt by being slow and polite
await Task.Delay(2000); // 2 seconds between sites
```

## 🔧 **Customization Options**

### **Add New News Sources**

Create a new scraper implementing `INewsPortalScraper`:

```csharp
public class BBCNewsScraper : INewsPortalScraper
{
    public string PortalName => "BBC Technology";
    
    public async Task<List<NewsArticle>> ScrapeNewsAsync(IWebDriver driver, int maxArticles = 10)
    {
        driver.Navigate().GoToUrl("https://www.bbc.com/news/technology");
        
        var headlines = driver.FindElements(By.CssSelector("h2[data-testid='card-headline'] a"));
        
        // Process headlines...
        return articles;
    }
}

// Register in Program.cs:
builder.Services.AddSingleton<INewsPortalScraper, BBCNewsScraper>();
```

### **Customize Categories**

Modify the `CategorizeTitle` method to add your own categories:

```csharp
// Add gaming category
if (titleLower.Contains("gaming") || titleLower.Contains("esports") || 
    titleLower.Contains("steam"))
    return "Gaming";

// Add climate tech
if (titleLower.Contains("climate") || titleLower.Contains("renewable") || 
    titleLower.Contains("sustainability"))
    return "Climate Tech";
```

### **Adjust Scraping Frequency**

For production use, wrap in a timer:

```csharp
// Run every hour
var timer = new Timer(async _ => await RunNewsAggregation(), 
    null, TimeSpan.Zero, TimeSpan.FromHours(1));
```

## 🚨 **Troubleshooting**

### **ChromeDriver Issues**
```bash
# Install ChromeDriver manually if needed:
# 1. Download from: https://chromedriver.chromium.org/
# 2. Put in project folder or add to PATH
# 3. Make sure Chrome browser is installed

# Or use automatic ChromeDriver:
dotnet add package Selenium.WebDriver.ChromeDriver
```

### **Site Layout Changes**
- Update CSS selectors in scrapers when sites redesign
- Add multiple backup selectors for robustness
- Monitor logs for "element not found" errors

### **Rate Limiting**
- Increase delays between requests: `await Task.Delay(5000)`
- Add random delays: `await Task.Delay(Random.Shared.Next(2000, 8000))`
- Use proxy rotation for high-volume scraping

## 🎯 **Why This Example Rocks**

1. **📊 Real Data** - Actually scrapes live websites, not mock APIs
2. **🤖 Smart Processing** - Intelligent categorization and prioritization  
3. **💪 Production Ready** - Proper error handling, logging, and resource cleanup
4. **🔧 Easily Extensible** - Add new sites with simple interface implementation
5. **⚡ Practical Value** - Solves the real problem of information overload
6. **📱 Modern Tech Stack** - Selenium 4 + .NET 8 + Chrome WebDriver

## 🚀 **Next Steps for Production**

- **📅 Schedule regular runs** (cron jobs, Azure Functions)
- **📧 Email digest generation** with HTML templates
- **📊 Trending analysis** using EYWA data
- **🔍 Add more news sources** (Product Hunt, ArsTechnica, etc.)
- **🤖 Machine learning categorization** for better accuracy
- **📱 Mobile app** for digest consumption

---

**This robot demonstrates the power of Selenium for real-world web automation** - no APIs required, just pure web scraping magic! 🔥
