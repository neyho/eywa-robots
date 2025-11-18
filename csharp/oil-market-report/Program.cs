using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EywaClient;
using EywaClient.Core;
using OilMarketReport.Models;
using OilMarketReport.Services;
using Newtonsoft.Json.Linq;

namespace OilMarketReport;

/// <summary>
/// Oil Market Intelligence Robot
/// Scrapes oil prices and exchange rates, generates Excel reports, sends email alerts
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
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
            if (config.SendEmailAlways || marketData.HasAlerts)
            {
                await eywa.Logger.InfoAsync("Phase 4: Sending email notification");
                await SendEmailNotification(eywa, marketData, reportPath, config);
                await eywa.Logger.InfoAsync($"✓ Email sent to {config.EmailRecipients.Count} recipient(s)");
            }
            else
            {
                await eywa.Logger.InfoAsync("Phase 4: Skipping email (no alerts and sendEmailAlways=false)");
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
    static RobotConfig ParseConfiguration(Dictionary<string, object> task)
    {
        Console.WriteLine("[Config] Parsing task configuration...");
        
        // EYWA tasks have 'data' field, not 'input'
        var data = task["data"] as JObject ?? new JObject();
        
        var config = new RobotConfig
        {
            // Alert thresholds
            AlertConfig = new AlertConfig
            {
                BrentHighThreshold = data["alertThresholds"]?["brentHigh"]?.Value<decimal>() ?? 85.0m,
                BrentLowThreshold = data["alertThresholds"]?["brentLow"]?.Value<decimal>() ?? 75.0m,
                DailyChangePercentThreshold = data["alertThresholds"]?["dailyChangePercent"]?.Value<decimal>() ?? 2.0m,
                UsdRsdChangeThreshold = data["alertThresholds"]?["usdRsdChangePercent"]?.Value<decimal>() ?? 1.0m
            },
            
            // Email configuration
            EmailRecipients = data["emailRecipients"]?.ToObject<List<string>>() ?? new List<string>(),
            SendEmailAlways = data["sendEmailAlways"]?.Value<bool>() ?? false,
            
            // Browser configuration
            Headless = data["headless"]?.Value<bool>() ?? false,
            
            // Output configuration
            OutputPath = data["outputPath"]?.Value<string>() ?? "/tmp/PetrolMarket_{date}.xlsx",
            
            // SMTP configuration (will be implemented in Phase 6)
            SmtpHost = data["smtp"]?["host"]?.Value<string>() ?? "smtp.mailtrap.io",
            SmtpPort = data["smtp"]?["port"]?.Value<int>() ?? 587,
            SmtpUsername = data["smtp"]?["username"]?.Value<string>() ?? "",
            SmtpPassword = data["smtp"]?["password"]?.Value<string>() ?? "",
            
            // Retry configuration
            RetryAttempts = data["retryAttempts"]?.Value<int>() ?? 3,
            RetryDelaySeconds = data["retryDelaySeconds"]?.Value<int>() ?? 5
        };
        
        Console.WriteLine($"[Config] Loaded: {config.EmailRecipients.Count} recipients, alerts={config.SendEmailAlways}, headless={config.Headless}");
        
        return config;
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
            var brent = await scraper.ScrapeBrentPrice();
            marketData.OilPrices.Add(brent);
            await eywa.Logger.InfoAsync($"Brent: ${brent.CurrentPrice} ({brent.DailyChangePercent:+0.00;-0.00}%)");
            
            // Scrape WTI Crude
            await eywa.Logger.InfoAsync("Scraping WTI Crude prices...");
            var wti = await scraper.ScrapeWTIPrice();
            marketData.OilPrices.Add(wti);
            await eywa.Logger.InfoAsync($"WTI: ${wti.CurrentPrice} ({wti.DailyChangePercent:+0.00;-0.00}%)");
            
            // Scrape EUR/USD
            await eywa.Logger.InfoAsync("Scraping EUR/USD exchange rate...");
            var eurUsd = await scraper.ScrapeEurUsd();
            marketData.ExchangeRates.Add(eurUsd);
            await eywa.Logger.InfoAsync($"EUR/USD: {eurUsd.CurrentRate:F4}");
            
            // Scrape USD/RSD
            await eywa.Logger.InfoAsync("Scraping USD/RSD exchange rate...");
            var usdRsd = await scraper.ScrapeUsdRsd();
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
    /// Generate Excel report (TO BE IMPLEMENTED IN PHASE 5)
    /// </summary>
    static async Task<string> GenerateExcelReport(Eywa eywa, MarketData data, RobotConfig config)
    {
        // TODO: Phase 5 - Implement Excel generation with EPPlus
        await eywa.Logger.WarnAsync("⚠️ Excel generation not yet implemented - using stub");
        
        var outputPath = config.OutputPath.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
        await eywa.Logger.InfoAsync($"Excel report would be generated at: {outputPath}");
        
        return outputPath;
    }
    
    /// <summary>
    /// Send email notification (TO BE IMPLEMENTED IN PHASE 6)
    /// </summary>
    static async Task SendEmailNotification(Eywa eywa, MarketData data, string reportPath, RobotConfig config)
    {
        // TODO: Phase 6 - Implement email sending with MailKit
        await eywa.Logger.WarnAsync("⚠️ Email sending not yet implemented - using stub");
        await eywa.Logger.InfoAsync($"Email would be sent to: {string.Join(", ", config.EmailRecipients)}");
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
    public int RetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
}
