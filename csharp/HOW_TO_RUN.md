# 🚀 Running EYWA C# Robots with `eywa run`

## 📋 **Prerequisites**

Before running robots with `eywa run`, ensure you have:

1. **✅ EYWA CLI installed and connected**
2. **✅ .NET 8.0 SDK installed**
3. **✅ Google Chrome browser** (for news aggregator)
4. **✅ Microsoft Outlook** (for email organizer, Windows only)

## 🎯 **Basic `eywa run` Command Structure**

```bash
eywa run [OPTIONS] -c "COMMAND"

# Options:
# --task-file    : Specify task configuration file
# --task-json    : Pass task configuration as JSON string
# -c             : Command to execute
```

## 🔧 **Step-by-Step Execution**

### **1. Navigate to Robot Directory**

```bash
# For News Aggregator
cd /Users/robi/dev/eywa-robots/csharp/news-aggregator

# For Email Organizer  
cd /Users/robi/dev/eywa-robots/csharp/email-organizer
```

### **2. Build the Robot (First Time Only)**

```bash
# Restore NuGet packages and build
dotnet restore
dotnet build
```

### **3. Run with EYWA Integration**

#### **Option A: Simple Run**
```bash
# Basic execution with EYWA connection
eywa run -c "dotnet run"
```

#### **Option B: With Task File**
```bash
# Run with specific task configuration
eywa run --task-file task.json -c "dotnet run"
```

#### **Option C: With Task JSON**
```bash
# Pass parameters directly
eywa run --task-json '{"action":"aggregate_news","headless":true}' -c "dotnet run"
```

## 📰 **News Aggregator Robot Example**

### **Full Workflow:**

```bash
# 1. Navigate to robot
cd /Users/robi/dev/eywa-robots/csharp/news-aggregator

# 2. Build (first time only)
dotnet restore
dotnet build

# 3. Run with EYWA
eywa run --task-file task.json -c "dotnet run"
```

### **Expected Output:**
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

📊 YOUR PERSONALIZED NEWS DIGEST
================================================================================
📁 AI & ML (4 articles)
🔥 OpenAI announces GPT-5 with unprecedented capabilities
   🏠 Hacker News | 👍 1247 points

📈 Total articles: 23 | 🔥 High priority: 6
✨ News aggregation complete!
```

## 📧 **Email Organizer Robot Example**

### **Full Workflow (Windows Only):**

```bash
# 1. Navigate to robot
cd /Users/robi/dev/eywa-robots/csharp/email-organizer

# 2. Build (first time only)
dotnet restore  
dotnet build

# 3. Run with EYWA
eywa run --task-file task.json -c "dotnet run"
```

### **Expected Output:**
```
📧 EYWA Email Organization Robot - REAL OUTLOOK EDITION! 🚀
================================================================

🔌 Connecting to Microsoft Outlook...
✅ Successfully connected to Outlook!
📧 Inbox: Inbox (127 items)

📊 REAL OUTLOOK EMAIL PROCESSING SUMMARY:
#1. 🚨 URGENT: Server Down → Urgent (Priority 5)
#2. Team Meeting Tomorrow → Meeting (Priority 3)  
#3. LinkedIn Newsletter → Newsletter (Priority 1)

📈 Total processed: 10 REAL emails from Outlook
⚡ High priority items: 2
✨ Email organization complete!
```

## 🔧 **How EYWA Integration Works**

When you run `eywa run -c "dotnet run"`, here's what happens:

### **1. Environment Setup**
```bash
# EYWA automatically sets environment variables:
export EYWA_URL="http://localhost:8080"
export EYWA_ACCESS_TOKEN="your-token"
export EYWA_REFRESH_TOKEN="your-refresh-token"
```

### **2. Robot Connection**
```csharp
// Your C# robot automatically connects to EYWA:
var eywaClient = new Eywa();
await eywaClient.Connect(); // Uses environment variables
```

### **3. Data Storage**
```csharp
// Robot stores results in EYWA:
var mutation = @"
    mutation StoreNewsArticle($input: NewsArticleInput!) {
        syncNewsArticle(data: $input) {
            euuid
            title
        }
    }";
await _eywaClient.GraphQL(mutation, variables);
```

### **4. Task Configuration**
```json
// task.json is available to your robot:
{
  "action": "aggregate_news",
  "sources": ["hackernews", "techcrunch"],
  "headless": true
}
```

## 🎯 **Task Configuration Examples**

### **News Aggregator Tasks:**

```json
// Basic news aggregation
{
  "action": "aggregate_news",
  "headless": true,
  "max_articles_per_source": 10
}

// Specific sources only
{
  "action": "aggregate_news", 
  "sources": ["hackernews"],
  "categories_filter": ["AI", "Startups"]
}

// Production mode with monitoring
{
  "action": "aggregate_news",
  "headless": true,
  "store_in_eywa": true,
  "send_digest_email": true,
  "recipient": "user@company.com"
}
```

### **Email Organizer Tasks:**

```json
// Basic email organization  
{
  "action": "organize_emails",
  "max_emails": 20,
  "test_mode": false
}

// Safe mode (don't move emails)
{
  "action": "organize_emails",
  "max_emails": 10,
  "test_mode": true,
  "move_emails": false
}

// Specific categories only
{
  "action": "organize_emails",
  "categories": ["Urgent", "Customer", "Finance"],
  "priority_threshold": 3
}
```

## 🛠️ **Troubleshooting**

### **Common Issues:**

#### **"dotnet not found"**
```bash
# Install .NET 8.0 SDK:
# macOS: brew install dotnet
# Windows: Download from https://dotnet.microsoft.com/download
```

#### **"ChromeDriver not found"**
```bash
# Install Chrome browser first, then:
dotnet add package Selenium.WebDriver.ChromeDriver
```

#### **"Failed to connect to EYWA"**
```bash
# Check EYWA connection:
eywa status

# Reconnect if needed:  
eywa connect http://localhost:8080
```

#### **"Outlook COM error"**
```bash
# Windows only - ensure:
# 1. Microsoft Outlook is installed
# 2. At least one email account configured
# 3. Outlook is running
# 4. Run as Administrator if needed
```

## 🔄 **Scheduling Robots**

### **Run every hour:**
```bash
# Add to crontab (macOS/Linux)
0 * * * * cd /path/to/robot && eywa run -c "dotnet run"

# Or Windows Task Scheduler:
# Program: eywa
# Arguments: run -c "dotnet run"  
# Start in: C:\path\to\robot
```

### **Run on startup:**
```bash
# Create systemd service (Linux)
[Unit]
Description=EYWA News Robot
After=network.target

[Service]
Type=oneshot
ExecStart=/usr/local/bin/eywa run -c "dotnet run"
WorkingDirectory=/path/to/robot

[Install]  
WantedBy=multi-user.target
```

## 🚀 **Advanced Usage**

### **Multiple Robots in Sequence:**
```bash
# Run multiple robots one after another
eywa run -c "cd news-aggregator && dotnet run"
eywa run -c "cd email-organizer && dotnet run"  
```

### **Parallel Execution:**
```bash
# Run multiple robots simultaneously
eywa run -c "cd news-aggregator && dotnet run" &
eywa run -c "cd email-organizer && dotnet run" &
wait
```

### **Custom Environment:**
```bash
# Pass custom configuration
CUSTOM_CONFIG="production" eywa run -c "dotnet run"
```

## 🎯 **Quick Commands Reference**

```bash
# News Aggregator
cd news-aggregator
eywa run --task-json '{"headless":true}' -c "dotnet run"

# Email Organizer (Windows)
cd email-organizer  
eywa run --task-json '{"test_mode":true}' -c "dotnet run"

# Build and run
dotnet build && eywa run -c "dotnet run"

# Check EYWA status
eywa status

# View robot logs
eywa run -c "dotnet run" 2>&1 | tee robot.log
```

---

**The key insight**: `eywa run` automatically provides authentication and environment setup, so your C# robots can focus on automation logic while EYWA handles the platform integration! 🚀
