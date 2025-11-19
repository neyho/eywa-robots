using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using EywaClient;
using EywaClient.Core;
using OilMarketReport.Models;
using OilMarketReport.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using MailKit.Net.Smtp;
using MimeKit;

namespace OilMarketReport;

/// <summary>
/// Oil Market Intelligence Robot
/// Scrapes oil prices and exchange rates, generates Excel reports, sends email alerts
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // EPPlus requires license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var eywa = new Eywa();
        
        try
        {
            // Initialize EYWA communication pipe
            eywa.OpenPipe();
            
            // Get task from EYWA server
            var task = await eywa.Tasks.GetTaskAsync();
            await eywa.Logger.InfoAsync("Oil Market Intelligence Robot started");
            
            // Parse task configuration from 'data' field (EYWA Task structure)
            var config = ParseConfiguration(task);
            
            // Update task status to PROCESSING
            await eywa.Tasks.UpdateTaskAsync(Status.Processing);
            
            // ==========================================
            // PHASE 1: COLLECT MARKET DATA
            // ==========================================
            await eywa.Logger.InfoAsync("Phase 1: Collecting market data from web sources");
            var marketData = await CollectMarketData(eywa, config);
            
            // ==========================================
            // PHASE 2: ANALYZE DATA & CHECK ALERTS
            // ==========================================
            await eywa.Logger.InfoAsync("Phase 2: Analyzing data and checking alert thresholds");
            marketData.Alerts = CheckAlerts(marketData, config);
            
            if (marketData.HasAlerts)
            {
                await eywa.Logger.WarnAsync($"⚠️ {marketData.Alerts.Count} alert(s) triggered");
                foreach (var alert in marketData.Alerts)
                {
                    await eywa.Logger.WarnAsync($"  • {alert}");
                }
            }
            else
            {
                await eywa.Logger.InfoAsync("✓ No alerts triggered - all prices within normal ranges");
            }
            
            // ==========================================
            // PHASE 3: GENERATE EXCEL REPORT
            // ==========================================
            await eywa.Logger.InfoAsync("Phase 3: Generating Excel report");
            var reportPath = await GenerateExcelReport(eywa, marketData, config);
            await eywa.Logger.InfoAsync($"✓ Excel report generated: {reportPath}");
            
            // ==========================================
            // PHASE 4: SEND EMAIL NOTIFICATION
            // ==========================================
            if (config.EmailRecipients.Count > 0)
            {
                await eywa.Logger.InfoAsync("Phase 4: Sending email notification");
                await SendEmailNotification(eywa, marketData, reportPath, config);
                await eywa.Logger.InfoAsync($"✓ Email sent to {config.EmailRecipients.Count} recipient(s)");
            }
            else
            {
                await eywa.Logger.InfoAsync("Phase 4: Skipping email (no recipients configured)");
            }
            
            // ==========================================
            // REPORT SUCCESS TO EYWA
            // ==========================================
            await eywa.Tasks.ReportAsync("Market Data Collected", new ReportOptions
            {
                Data = new ReportData
                {
                    Card = $"""
                        # Oil Market Data Collection Complete ✅
                        
                        ## Prices Collected
                        - **Brent Crude:** ${marketData.Brent?.CurrentPrice:F2} ({marketData.Brent?.DailyChangePercent:+0.00;-0.00}%)
                        - **WTI Crude:** ${marketData.WTI?.CurrentPrice:F2} ({marketData.WTI?.DailyChangePercent:+0.00;-0.00}%)
                        - **EUR/USD:** {marketData.EurUsd?.CurrentRate:F4}
                        - **USD/RSD:** {marketData.UsdRsd?.CurrentRate:F2}
                        
                        ## Status
                        - **Alerts Triggered:** {marketData.Alerts.Count}
                        - **Report Generated:** {reportPath}
                        """
                }
            });
            
            await eywa.Logger.InfoAsync("✓ Robot completed successfully");
            await eywa.Tasks.CloseTaskAsync(Status.Success);
        }
        catch (Exception ex)
        {
            // Handle any errors and report to EYWA
            await eywa.Logger.ErrorAsync($"❌ Robot failed: {ex.Message}", new { stackTrace = ex.StackTrace });
            await eywa.Tasks.CloseTaskAsync(Status.Error);
        }
    }
    
    /// <summary>
    /// Parse task data JSON into typed configuration
    /// EYWA Task structure: { euuid, message, type, priority, data: {...} }
    /// </summary>
    static RobotConfig ParseConfiguration(JsonNode? task)
    {
        Console.WriteLine("[Config] Parsing task configuration...");

        // Load configuration file
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var configJson = File.ReadAllText(configPath);
        var configFile = JsonNode.Parse(configJson);

        // EYWA task data (contains only variable parts like email recipients)
        var taskData = task?["data"];

        var config = new RobotConfig
        {
            // Alert thresholds from config file
            AlertConfig = new AlertConfig
            {
                BrentHighThreshold = configFile?["alertThresholds"]?["brentPriceChangePercent"]?.GetValue<decimal>() ?? 5.0m,
                BrentLowThreshold = configFile?["alertThresholds"]?["brentPriceChangePercent"]?.GetValue<decimal>() ?? 5.0m,
                DailyChangePercentThreshold = configFile?["alertThresholds"]?["wtiPriceChangePercent"]?.GetValue<decimal>() ?? 5.0m,
                UsdRsdChangeThreshold = configFile?["alertThresholds"]?["usdRsdChangePercent"]?.GetValue<decimal>() ?? 1.0m
            },

            // Email recipients from task data (the only variable part)
            EmailRecipients = ParseStringList(taskData?["emailRecipients"]),
            SendEmailAlways = true, // Always send email if recipients exist

            // Browser configuration - always show browser for debugging
            Headless = false,

            // Output path from config file
            OutputPath = configFile?["outputPath"]?.GetValue<string>() ?? "C:/tmp/PetrolMarket_{timestamp}.xlsx",

            // SMTP configuration from environment variables
            SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.mailtrap.io",
            SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587,
            SmtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "",
            SmtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "",
            SmtpFromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "robot@eywa.example.com",
            SmtpFromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "EYWA Oil Market Robot",

            // Retry configuration - hardcoded defaults
            RetryAttempts = 3,
            RetryDelaySeconds = 5,

            // Data source URLs from config file
            BrentUrl = configFile?["datasources"]?["brentUrl"]?.GetValue<string>() ?? "https://www.investing.com/commodities/brent-oil",
            WtiUrl = configFile?["datasources"]?["wtiUrl"]?.GetValue<string>() ?? "https://www.investing.com/commodities/crude-oil",
            EurUsdUrl = configFile?["datasources"]?["eurUsdUrl"]?.GetValue<string>() ?? "https://www.investing.com/currencies/eur-usd",
            UsdRsdUrl = configFile?["datasources"]?["usdRsdUrl"]?.GetValue<string>() ?? "https://webappcenter.nbs.rs/ExchangeRateWebApp/ExchangeRateRsd/IndexNew_Partial_IndikativniKurs"
        };

        Console.WriteLine($"[Config] Loaded: {config.EmailRecipients.Count} recipients, alerts={config.SendEmailAlways}, headless={config.Headless}");

        return config;
    }

    /// <summary>
    /// Parse JsonNode array to List of strings
    /// </summary>
    static List<string> ParseStringList(JsonNode? node)
    {
        var result = new List<string>();
        if (node == null) return result;

        var array = node.AsArray();
        foreach (var item in array)
        {
            var value = item?.GetValue<string>();
            if (!string.IsNullOrEmpty(value))
                result.Add(value);
        }

        return result;
    }

    /// <summary>
    /// Collect market data from web sources using Selenium
    /// </summary>
    static async Task<MarketData> CollectMarketData(Eywa eywa, RobotConfig config)
    {
        var marketData = new MarketData
        {
            CollectionTime = DateTime.Now,
            OilPrices = new List<OilPrice>(),
            ExchangeRates = new List<ExchangeRate>()
        };
        
        using (var scraper = new WebScraperService(config.Headless, config.RetryAttempts, config.RetryDelaySeconds))
        {
            // Scrape Brent Crude
            await eywa.Logger.InfoAsync("Scraping Brent Crude prices...");
            var brent = await scraper.ScrapeBrentPrice(config.BrentUrl);
            marketData.OilPrices.Add(brent);
            await eywa.Logger.InfoAsync($"Brent: ${brent.CurrentPrice} ({brent.DailyChangePercent:+0.00;-0.00}%)");

            // Scrape WTI Crude
            await eywa.Logger.InfoAsync("Scraping WTI Crude prices...");
            var wti = await scraper.ScrapeWTIPrice(config.WtiUrl);
            marketData.OilPrices.Add(wti);
            await eywa.Logger.InfoAsync($"WTI: ${wti.CurrentPrice} ({wti.DailyChangePercent:+0.00;-0.00}%)");

            // Scrape EUR/USD
            await eywa.Logger.InfoAsync("Scraping EUR/USD exchange rate...");
            var eurUsd = await scraper.ScrapeEurUsd(config.EurUsdUrl);
            marketData.ExchangeRates.Add(eurUsd);
            await eywa.Logger.InfoAsync($"EUR/USD: {eurUsd.CurrentRate:F4}");

            // Scrape USD/RSD
            await eywa.Logger.InfoAsync("Scraping USD/RSD exchange rate...");
            var usdRsd = await scraper.ScrapeUsdRsd(config.UsdRsdUrl);
            marketData.ExchangeRates.Add(usdRsd);
            await eywa.Logger.InfoAsync($"USD/RSD: {usdRsd.CurrentRate:F2}");
        }
        
        return marketData;
    }
    
    /// <summary>
    /// Check if any alert thresholds are crossed
    /// </summary>
    static List<string> CheckAlerts(MarketData data, RobotConfig config)
    {
        var alerts = new List<string>();
        
        // Check Brent price thresholds
        var brent = data.Brent;
        if (brent != null)
        {
            if (brent.CurrentPrice > config.AlertConfig.BrentHighThreshold)
                alerts.Add($"Brent above ${config.AlertConfig.BrentHighThreshold} threshold (current: ${brent.CurrentPrice})");
            
            if (brent.CurrentPrice < config.AlertConfig.BrentLowThreshold)
                alerts.Add($"Brent below ${config.AlertConfig.BrentLowThreshold} threshold (current: ${brent.CurrentPrice})");
            
            if (Math.Abs(brent.DailyChangePercent) > config.AlertConfig.DailyChangePercentThreshold)
                alerts.Add($"Brent daily change >{config.AlertConfig.DailyChangePercentThreshold}% (current: {brent.DailyChangePercent:+0.00;-0.00}%) - consider hedging");
        }
        
        // Check WTI price changes
        var wti = data.WTI;
        if (wti != null && Math.Abs(wti.DailyChangePercent) > config.AlertConfig.DailyChangePercentThreshold)
        {
            alerts.Add($"WTI daily change >{config.AlertConfig.DailyChangePercentThreshold}% (current: {wti.DailyChangePercent:+0.00;-0.00}%)");
        }
        
        // Check USD/RSD exchange rate
        var usdRsd = data.UsdRsd;
        if (usdRsd != null && Math.Abs(usdRsd.DailyChangePercent) > config.AlertConfig.UsdRsdChangeThreshold)
        {
            alerts.Add($"USD/RSD moved >{config.AlertConfig.UsdRsdChangeThreshold}% (current: {usdRsd.DailyChangePercent:+0.00;-0.00}%) - impacts import costs");
        }
        
        return alerts;
    }
    
    /// <summary>
    /// Generate Excel report with market data
    /// </summary>
    static async Task<string> GenerateExcelReport(Eywa eywa, MarketData data, RobotConfig config)
    {
        var outputPath = config.OutputPath.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Market Summary");

        // Title
        sheet.Cells[1, 1].Value = "Oil Market Intelligence Report";
        sheet.Cells[1, 1].Style.Font.Size = 16;
        sheet.Cells[1, 1].Style.Font.Bold = true;

        // Timestamp
        sheet.Cells[2, 1].Value = $"Generated: {data.CollectionTime:yyyy-MM-dd HH:mm:ss}";

        // Oil Prices
        int row = 4;
        sheet.Cells[row, 1].Value = "OIL PRICES";
        sheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        sheet.Cells[row, 1].Value = "Product";
        sheet.Cells[row, 2].Value = "Price";
        sheet.Cells[row, 3].Value = "Change";
        sheet.Cells[row, 4].Value = "Change %";
        sheet.Cells[row, 1, row, 4].Style.Font.Bold = true;
        row++;

        if (data.Brent != null)
        {
            sheet.Cells[row, 1].Value = "Brent";
            sheet.Cells[row, 2].Value = data.Brent.CurrentPrice;
            sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells[row, 3].Value = data.Brent.DailyChange;
            sheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells[row, 4].Value = data.Brent.DailyChangePercent / 100;
            sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
            row++;
        }

        if (data.WTI != null)
        {
            sheet.Cells[row, 1].Value = "WTI";
            sheet.Cells[row, 2].Value = data.WTI.CurrentPrice;
            sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells[row, 3].Value = data.WTI.DailyChange;
            sheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells[row, 4].Value = data.WTI.DailyChangePercent / 100;
            sheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
            row++;
        }

        // Exchange Rates
        row += 2;
        sheet.Cells[row, 1].Value = "EXCHANGE RATES";
        sheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        sheet.Cells[row, 1].Value = "Pair";
        sheet.Cells[row, 2].Value = "Rate";
        sheet.Cells[row, 3].Value = "Change %";
        sheet.Cells[row, 1, row, 3].Style.Font.Bold = true;
        row++;

        if (data.EurUsd != null)
        {
            sheet.Cells[row, 1].Value = "EUR/USD";
            sheet.Cells[row, 2].Value = data.EurUsd.CurrentRate;
            sheet.Cells[row, 2].Style.Numberformat.Format = "0.0000";
            sheet.Cells[row, 3].Value = data.EurUsd.DailyChangePercent / 100;
            sheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";
            row++;
        }

        if (data.UsdRsd != null)
        {
            sheet.Cells[row, 1].Value = "USD/RSD";
            sheet.Cells[row, 2].Value = data.UsdRsd.CurrentRate;
            sheet.Cells[row, 2].Style.Numberformat.Format = "0.0000";
            sheet.Cells[row, 3].Value = data.UsdRsd.DailyChangePercent / 100;
            sheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";
            row++;
        }

        // Auto-fit columns
        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

        // Save to local file
        var fileInfo = new FileInfo(outputPath);
        if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
        {
            fileInfo.Directory.Create();
        }

        await package.SaveAsAsync(fileInfo);
        await eywa.Logger.InfoAsync($"Excel report saved locally: {outputPath} ({fileInfo.Length} bytes)");

        // Upload to EYWA filesystem
        /* 
        var folderUuid = await EnsureEywaReportFolderExists(eywa);
        var fileName = Path.GetFileName(outputPath);
        var fileUuid = Guid.NewGuid().ToString();
        var year = DateTime.Now.Year.ToString();
        var month = DateTime.Now.Month.ToString("D2");
        var eywaPath = $"/demo/oil-market/{year}/{month}/{fileName}";

        using var memoryStream = new MemoryStream();
        await package.SaveAsAsync(memoryStream);
        memoryStream.Position = 0;

        await eywa.Files.UploadStreamAsync(memoryStream, new Dictionary<string, object>
        {
            ["euuid"] = fileUuid,
            ["name"] = fileName,
            ["size"] = memoryStream.Length,
            ["folder"] = new Dictionary<string, object> { ["euuid"] = folderUuid },
            ["content_type"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        });

        await eywa.Logger.InfoAsync($"✅ Excel uploaded to EYWA: {eywaPath}", new { fileUuid, size = memoryStream.Length });
        */

        return outputPath;
    }

    /// <summary>
    /// Ensure the EYWA folder path /demo/oil-market/YYYY/MM/ exists
    /// </summary>
    static async Task<string> EnsureEywaReportFolderExists(Eywa eywa)
    {
        var year = DateTime.Now.Year.ToString();
        var month = DateTime.Now.Month.ToString("D2");
        var pathComponents = new[] { "demo", "oil-market", year, month };

        var (parentUuid, matchedDepth) = await FindDeepestMatchingFolder(eywa, pathComponents);

        if (matchedDepth == pathComponents.Length)
        {
            return parentUuid;
        }

        var currentParent = parentUuid;
        for (int i = matchedDepth; i < pathComponents.Length; i++)
        {
            var folderName = pathComponents[i];
            var folderUuid = Guid.NewGuid().ToString();

            await eywa.Files.CreateFolderAsync(new Dictionary<string, object>
            {
                ["euuid"] = folderUuid,
                ["name"] = folderName,
                ["parent"] = new Dictionary<string, object> { ["euuid"] = currentParent }
            });

            currentParent = folderUuid;
        }

        return currentParent;
    }

    /// <summary>
    /// Find the deepest existing folder matching the target path
    /// </summary>
    static async Task<(string folderUuid, int matchedDepth)> FindDeepestMatchingFolder(
        Eywa eywa, string[] pathComponents)
    {
        for (int depth = pathComponents.Length; depth > 0; depth--)
        {
            var checkPath = "/" + string.Join("/", pathComponents.Take(depth));
            if (checkPath != "/")
            {
                checkPath += "/";
            }

            var query = @"
                query GetFolder($path: String!) {
                    getFolder(path: $path) {
                        euuid name path
                    }
                }";

            try
            {
                var result = await eywa.GraphQLAsync(query, new { path = checkPath });
                var folderNode = result?["data"]?["getFolder"];
                if (folderNode != null)
                {
                    var folderUuid = folderNode["euuid"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
                    return (folderUuid, depth);
                }
            }
            catch
            {
                continue;
            }
        }

        return (eywa.Files.RootUuid, 0);
    }
    
    /// <summary>
    /// Send email notification (TO BE IMPLEMENTED IN PHASE 6)
    /// </summary>
    static async Task SendEmailNotification(Eywa eywa, MarketData data, string reportPath, RobotConfig config)
    {
        try
        {
            await eywa.Logger.InfoAsync($"Preparing email for {config.EmailRecipients.Count} recipient(s)");

            // Create email message
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(config.SmtpFromName, config.SmtpFromEmail));

            foreach (var recipient in config.EmailRecipients)
            {
                message.To.Add(MimeKit.MailboxAddress.Parse(recipient));
            }

            // Email subject with alert indicator
            var subject = data.HasAlerts
                ? "⚠️ Oil Market Report - ALERTS TRIGGERED"
                : "Oil Market Daily Report";
            message.Subject = $"{subject} - {data.CollectionTime:yyyy-MM-dd}";

            // Build email body
            var bodyBuilder = new MimeKit.BodyBuilder();
            bodyBuilder.HtmlBody = BuildEmailHtmlBody(data);
            bodyBuilder.TextBody = BuildEmailTextBody(data);

            // Attach Excel report
            if (File.Exists(reportPath))
            {
                await bodyBuilder.Attachments.AddAsync(reportPath);
                await eywa.Logger.InfoAsync($"Attached Excel report: {Path.GetFileName(reportPath)}");
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send email via SMTP
            using var client = new MailKit.Net.Smtp.SmtpClient();

            await eywa.Logger.InfoAsync($"Connecting to SMTP server: {config.SmtpHost}:{config.SmtpPort}");
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(config.SmtpUsername) && !string.IsNullOrEmpty(config.SmtpPassword))
            {
                await eywa.Logger.InfoAsync($"Authenticating as: {config.SmtpUsername}");
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword);
            }

            await eywa.Logger.InfoAsync("Sending email...");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            await eywa.Logger.InfoAsync($"✓ Email sent successfully to: {string.Join(", ", config.EmailRecipients)}");
        }
        catch (Exception ex)
        {
            await eywa.Logger.ErrorAsync($"Failed to send email: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Build HTML email body with market data summary
    /// </summary>
    static string BuildEmailHtmlBody(MarketData data)
    {
        var alertSection = "";
        if (data.HasAlerts)
        {
            alertSection = $@"
                <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin-bottom: 20px;'>
                    <h3 style='margin: 0 0 10px 0; color: #856404;'>⚠️ Alerts Triggered</h3>
                    <ul style='margin: 0; padding-left: 20px;'>
                        {string.Join("", data.Alerts.Select(a => $"<li style='color: #856404;'>{a}</li>"))}
                    </ul>
                </div>";
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .price-card {{ background: white; border-radius: 8px; padding: 15px; margin: 10px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .price-value {{ font-size: 24px; font-weight: bold; color: #007bff; }}
        .price-change {{ font-size: 16px; margin-top: 5px; }}
        .positive {{ color: #28a745; }}
        .negative {{ color: #dc3545; }}
        .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Oil Market Intelligence Report</h1>
            <p>{data.CollectionTime:MMMM dd, yyyy HH:mm}</p>
        </div>

        <div class='content'>
            {alertSection}

            <h2>Oil Prices</h2>
            <div class='price-card'>
                <h3>Brent Crude</h3>
                <div class='price-value'>${data.Brent?.CurrentPrice:F2}</div>
                <div class='price-change {(data.Brent?.DailyChange >= 0 ? "positive" : "negative")}'>
                    {(data.Brent?.DailyChange >= 0 ? "▲" : "▼")} ${Math.Abs(data.Brent?.DailyChange ?? 0):F2} ({data.Brent?.DailyChangePercent:+0.00;-0.00}%)
                </div>
            </div>

            <div class='price-card'>
                <h3>WTI Crude</h3>
                <div class='price-value'>${data.WTI?.CurrentPrice:F2}</div>
                <div class='price-change {(data.WTI?.DailyChange >= 0 ? "positive" : "negative")}'>
                    {(data.WTI?.DailyChange >= 0 ? "▲" : "▼")} ${Math.Abs(data.WTI?.DailyChange ?? 0):F2} ({data.WTI?.DailyChangePercent:+0.00;-0.00}%)
                </div>
            </div>

            <h2>Exchange Rates</h2>
            <div class='price-card'>
                <h3>EUR/USD</h3>
                <div class='price-value'>{data.EurUsd?.CurrentRate:F4}</div>
                <div class='price-change {(data.EurUsd?.DailyChangePercent >= 0 ? "positive" : "negative")}'>
                    {(data.EurUsd?.DailyChangePercent >= 0 ? "▲" : "▼")} {Math.Abs(data.EurUsd?.DailyChangePercent ?? 0):F2}%
                </div>
            </div>

            <div class='price-card'>
                <h3>USD/RSD</h3>
                <div class='price-value'>{data.UsdRsd?.CurrentRate:F4}</div>
                <div class='price-change {(data.UsdRsd?.DailyChangePercent >= 0 ? "positive" : "negative")}'>
                    {(data.UsdRsd?.DailyChangePercent >= 0 ? "▲" : "▼")} {Math.Abs(data.UsdRsd?.DailyChangePercent ?? 0):F2}%
                </div>
            </div>
        </div>

        <div class='footer'>
            <p>📊 Detailed Excel report attached</p>
            <p>Generated by EYWA Oil Market Intelligence Robot</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Build plain text email body as fallback
    /// </summary>
    static string BuildEmailTextBody(MarketData data)
    {
        var alertSection = "";
        if (data.HasAlerts)
        {
            alertSection = $@"
⚠️ ALERTS TRIGGERED:
{string.Join("\n", data.Alerts.Select(a => $"  • {a}"))}

";
        }

        return $@"
OIL MARKET INTELLIGENCE REPORT
{data.CollectionTime:MMMM dd, yyyy HH:mm}
=====================================

{alertSection}
OIL PRICES:
-----------
Brent Crude: ${data.Brent?.CurrentPrice:F2}
  Change: {(data.Brent?.DailyChange >= 0 ? "+" : "")}{data.Brent?.DailyChange:F2} ({data.Brent?.DailyChangePercent:+0.00;-0.00}%)

WTI Crude: ${data.WTI?.CurrentPrice:F2}
  Change: {(data.WTI?.DailyChange >= 0 ? "+" : "")}{data.WTI?.DailyChange:F2} ({data.WTI?.DailyChangePercent:+0.00;-0.00}%)

EXCHANGE RATES:
---------------
EUR/USD: {data.EurUsd?.CurrentRate:F4}
  Change: {data.EurUsd?.DailyChangePercent:+0.00;-0.00}%

USD/RSD: {data.UsdRsd?.CurrentRate:F4}
  Change: {data.UsdRsd?.DailyChangePercent:+0.00;-0.00}%

=====================================
📊 See attached Excel report for detailed analysis
Generated by EYWA Oil Market Intelligence Robot
";
    }
}

/// <summary>
/// Robot configuration parsed from task data
/// </summary>
public class RobotConfig
{
    public AlertConfig AlertConfig { get; set; } = new();
    public List<string> EmailRecipients { get; set; } = new();
    public bool SendEmailAlways { get; set; }
    public bool Headless { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SmtpFromEmail { get; set; } = string.Empty;
    public string SmtpFromName { get; set; } = string.Empty;
    public int RetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }

    // Data source URLs
    public string BrentUrl { get; set; } = string.Empty;
    public string WtiUrl { get; set; } = string.Empty;
    public string EurUsdUrl { get; set; } = string.Empty;
    public string UsdRsdUrl { get; set; } = string.Empty;
}
