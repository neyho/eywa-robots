# 🛢️ Oil Market Intelligence Robot

**Automated Daily Oil Price Monitoring & Reporting for Naftna Industrija Srbije (NIS)**

---

## 📊 Business Case

### The Problem
Every morning, pricing analysts spend 45-60 minutes manually:
- Opening multiple websites to check oil prices
- Recording Brent Crude, WTI Crude prices
- Checking EUR/USD and USD/RSD exchange rates
- Calculating impacts on local pricing
- Creating Excel reports
- Emailing stakeholders
- **Human errors** in data entry or calculations
- **Inconsistent formats** across different analysts
- **No weekend/holiday coverage**

### The Solution
This EYWA robot automates the entire workflow in **3 minutes**, running unattended every morning at 7:00 AM.

### ROI Calculation
```
Manual Process:
• Analyst time: 45 min/day × 250 working days = 187.5 hours/year
• Analyst cost: €25/hour (Serbia market rate)
• Annual cost: €4,687.50

With EYWA Robot:
• Robot runs: 3 minutes (fully automated)
• Development: 1 day (one-time cost)
• Maintenance: ~2 hours/year
• Annual savings: €4,500+
• Payback period: < 1 month

Additional Benefits:
✅ 100% error elimination
✅ Consistent professional reports
✅ Weekend/holiday coverage
✅ Real-time alerts for urgent decisions
✅ Historical trend analysis
```

---

## 🎯 What This Robot Does

### Data Collection (Automated Web Scraping)
1. **Brent Crude Oil** - Current price, daily change, % change
2. **WTI Crude Oil** - Current price, daily change, % change
3. **EUR/USD Exchange Rate** - Impacts European pricing
4. **USD/RSD Exchange Rate** - Critical for local cost calculations

### Analysis & Alerts
- Calculates 7-day price averages
- Converts Brent price to RSD for local impact
- Triggers alerts when:
  - Brent crosses high/low thresholds ($85 / $75)
  - Daily price change exceeds 2%
  - USD/RSD moves more than 1%

### Professional Excel Report
**3 Sheets:**
1. **Daily Dashboard** - Executive summary with formatted tables
2. **7-Day History** - Price trends over time
3. **Calculation Details** - Raw data and methodology

**Formatting:**
- Color-coded changes (green/red)
- Currency symbols and decimal formatting
- Professional borders and styling
- Company branding ready

### Email Notifications
- Sends to configured recipients (pricing team, management)
- **Alert emails** for urgent thresholds
- **Regular emails** for daily updates (optional)
- Includes Excel report as attachment
- HTML-formatted with summary

---

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 or 9.0 SDK
- Chrome browser (for ChromeDriver)
- EYWA CLI installed and connected
- Internet connection

### Installation

1. **Navigate to project:**
   ```bash
   cd /Users/robi/dev/eywa-robots/csharp/oil-market-report
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build project:**
   ```bash
   dotnet build
   ```

### Configuration

Edit `task.json` to customize:

```json
{
  "input": {
    "alertThresholds": {
      "brentHigh": 85.0,          // Alert if Brent > $85
      "brentLow": 75.0,           // Alert if Brent < $75
      "dailyChangePercent": 2.0,  // Alert if daily change > 2%
      "usdRsdChangePercent": 1.0  // Alert if RSD moves > 1%
    },
    "emailRecipients": [
      "pricing-team@nis.rs",
      "management@nis.rs"
    ],
    "sendEmailAlways": false,     // true = send daily, false = only on alerts
    "headless": false,             // false = see browser, true = background
    "smtp": {
      "host": "smtp.office365.com",
      "port": 587,
      "username": "robot@nis.rs",
      "password": "your-password"
    }
  }
}
```

### Running the Robot

**Local testing with EYWA:**
```bash
eywa run --task-file task.json -c "dotnet run"
```

**Watch the robot:**
- Chrome browser opens and navigates to websites
- Console shows progress: "Scraping Brent Crude prices..."
- Prices are extracted and displayed
- Excel report is generated
- Email is sent (if configured)

**Output:**
- Excel report: `/tmp/PetrolMarket_2024-11-18.xlsx`
- Console logs show all extracted prices
- EYWA task report with summary data

---

## 📁 Project Structure

```
oil-market-report/
├── OilMarketReport.csproj       # Project dependencies
├── Program.cs                    # Main robot logic & EYWA lifecycle
├── task.json                     # Configuration for demo/testing
├── README.md                     # This file
├── IMPLEMENTATION_PLAN.md        # Detailed technical implementation
│
├── Models/                       # Data structures
│   ├── MarketData.cs            # Container for all market data
│   ├── OilPrice.cs              # Oil price data model
│   ├── ExchangeRate.cs          # Exchange rate data model
│   └── AlertConfig.cs           # Alert threshold configuration
│
├── Services/                     # Business logic
│   ├── WebScraperService.cs     # Selenium ChromeDriver scraping
│   ├── ExcelGeneratorService.cs # EPPlus Excel report generation
│   └── EmailService.cs          # MailKit email notifications
│
└── Utils/                        # Helper functions
    └── PriceCalculator.cs       # Price analysis & alert checking
```

---

## 🌐 Data Sources

### Oil Prices
- **Brent Crude:** [Investing.com - Brent Oil](https://www.investing.com/commodities/brent-oil)
- **WTI Crude:** [Investing.com - Crude Oil](https://www.investing.com/commodities/crude-oil)

### Exchange Rates
- **EUR/USD:** [Investing.com - EUR/USD](https://www.investing.com/currencies/eur-usd)
- **USD/RSD:** [National Bank of Serbia](https://www.nbs.rs/kursnaListaModul/srednjiKurs.faces)

**Note:** If any source fails, robot will retry 3 times with exponential backoff.

---

## 📧 Email Configuration

### Supported SMTP Providers
- **Office 365:** `smtp.office365.com:587`
- **Gmail:** `smtp.gmail.com:587` (requires App Password)
- **Custom SMTP:** Any SMTP server

### Email Format

**Subject Line:**
- Regular: `🛢️ Daily Petrol Market Report - 2024-11-18`
- With Alerts: `🛢️ Daily Petrol Market Report - 2024-11-18 - ⚠️ ALERT`

**Body Content:**
- Executive summary of key prices
- Alert notifications (if any)
- Local impact calculation (Brent in RSD)
- Excel report attachment

---

## 🎬 Demo Script (5 Minutes)

### Setup (30 seconds)
> "Every morning, your analysts need fresh oil prices and exchange rates. Let me show you how EYWA automates this."

### Live Demo (3 minutes)
1. **Show configuration** - `task.json` with thresholds
2. **Run robot** - `eywa run --task-file task.json -c "dotnet run"`
3. **Watch automation:**
   - Console: "Collecting Brent Crude prices..."
   - Chrome opens → Investing.com → Extracts price
   - "Brent: $85.20 (+1.79%)" displayed
   - Repeats for WTI, EUR/USD, USD/RSD
   - "⚠️ Alert detected: Brent above $85"
   - "Generating Excel report..."
   - "Sending email notification..."
   - "✅ Task completed successfully"

### Show Results (1 minute)
1. Open Excel report → Professional formatting, all data present
2. Check email inbox → Email with attachment received
3. Highlight alert section in both

### Close (30 seconds)
> "This runs every morning at 7 AM. Zero manual work. Consistent reports. Your team focuses on decisions, not data gathering. And this is just one robot - imagine automating competitor monitoring, regulatory news, inventory reports..."

---

## 🛠️ Troubleshooting

### Robot Fails to Scrape
**Problem:** Website changed layout, elements not found  
**Solution:** 
- Check browser visibility (set `headless: false`)
- Update CSS selectors in `WebScraperService.cs`
- Check if site added CAPTCHA or bot detection

### Excel Not Generated
**Problem:** EPPlus error or file permission issue  
**Solution:**
- Check `/tmp` directory permissions
- Verify EPPlus license configuration
- Try different output path in task.json

### Email Not Sent
**Problem:** SMTP authentication failure  
**Solution:**
- Verify SMTP credentials
- Check firewall/antivirus blocking port 587
- For Gmail: Generate App Password
- Test with mailtrap.io first

### Chrome Driver Issues
**Problem:** ChromeDriver version mismatch  
**Solution:**
- Selenium.WebDriver auto-downloads correct version
- Clear driver cache: `rm -rf ~/.selenium/`
- Update Selenium.WebDriver package

---

## 🔐 Security Considerations

### Credentials Storage
- **Do NOT** commit SMTP passwords to Git
- Use environment variables or EYWA secrets
- Rotate passwords regularly

### Data Privacy
- Market data is public information
- Be mindful of company-specific thresholds
- Consider encrypting Excel reports if emailing externally

---

## 📈 Future Enhancements

### Phase 2: Historical Analysis
- Store prices in EYWA dataset
- 30-day trend charts
- Predictive analytics

### Phase 3: Extended Coverage
- Natural gas prices
- Refined product prices (gasoline, diesel)
- Competitor pricing intelligence

### Phase 4: Advanced Notifications
- Telegram bot integration
- SMS alerts for critical thresholds
- Slack channel notifications

### Phase 5: Custom Reports
- Weekly summary reports
- Monthly executive briefings
- Custom date range analysis

---

## 📞 Support & Contact

**For technical questions about this robot:**
- Check `IMPLEMENTATION_PLAN.md` for detailed architecture
- Review code comments in each service
- Test with `task.json` in different scenarios

**For EYWA platform questions:**
- EYWA Documentation: [docs.eywaonline.com](https://docs.eywaonline.com)
- EYWA Support: support@eywaonline.com

---

## 📜 License

This example robot is provided as-is for demonstration purposes.

---

**Built with EYWA Platform** | **Powered by C# .NET** | **Designed for NIS**

*Automating the routine, so you can focus on decisions that matter.*
