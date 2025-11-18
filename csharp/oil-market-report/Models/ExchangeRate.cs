namespace OilMarketReport.Models;

/// <summary>
/// Represents exchange rate data for a currency pair
/// </summary>
public class ExchangeRate
{
    /// <summary>
    /// Currency pair: "EUR/USD", "USD/RSD"
    /// </summary>
    public string Pair { get; set; } = string.Empty;
    
    /// <summary>
    /// Current exchange rate
    /// </summary>
    public decimal CurrentRate { get; set; }
    
    /// <summary>
    /// Previous rate (from previous trading day)
    /// </summary>
    public decimal PreviousRate { get; set; }
    
    /// <summary>
    /// Daily change in absolute value
    /// </summary>
    public decimal DailyChange { get; set; }
    
    /// <summary>
    /// Daily change as percentage
    /// </summary>
    public decimal DailyChangePercent { get; set; }
    
    /// <summary>
    /// 7-day average rate (calculated from historical data)
    /// </summary>
    public decimal SevenDayAverage { get; set; }
    
    /// <summary>
    /// Timestamp when data was collected
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Source URL where data was scraped from
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
}
