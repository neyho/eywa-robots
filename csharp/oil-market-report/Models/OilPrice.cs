namespace OilMarketReport.Models;

/// <summary>
/// Represents oil price data for a specific product (Brent or WTI)
/// </summary>
public class OilPrice
{
    /// <summary>
    /// Product name: "Brent" or "WTI"
    /// </summary>
    public string Product { get; set; } = string.Empty;
    
    /// <summary>
    /// Current price in USD per barrel
    /// </summary>
    public decimal CurrentPrice { get; set; }
    
    /// <summary>
    /// Previous close price in USD per barrel
    /// </summary>
    public decimal PreviousClose { get; set; }
    
    /// <summary>
    /// Daily change in absolute USD value
    /// </summary>
    public decimal DailyChange { get; set; }
    
    /// <summary>
    /// Daily change as percentage
    /// </summary>
    public decimal DailyChangePercent { get; set; }
    
    /// <summary>
    /// 7-day average price (calculated from historical data)
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
    
    /// <summary>
    /// Market status: "Open", "Closed", "Unknown"
    /// </summary>
    public string MarketStatus { get; set; } = "Unknown";
}
