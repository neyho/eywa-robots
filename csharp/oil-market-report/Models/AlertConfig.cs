namespace OilMarketReport.Models;

/// <summary>
/// Alert threshold configuration
/// </summary>
public class AlertConfig
{
    /// <summary>
    /// Alert when Brent Crude exceeds this price (USD per barrel)
    /// Default: $85.00
    /// </summary>
    public decimal BrentHighThreshold { get; set; } = 85.0m;
    
    /// <summary>
    /// Alert when Brent Crude drops below this price (USD per barrel)
    /// Default: $75.00
    /// </summary>
    public decimal BrentLowThreshold { get; set; } = 75.0m;
    
    /// <summary>
    /// Alert when any daily price change exceeds this percentage
    /// Default: 2.0%
    /// </summary>
    public decimal DailyChangePercentThreshold { get; set; } = 2.0m;
    
    /// <summary>
    /// Alert when USD/RSD exchange rate changes by this percentage
    /// Default: 1.0%
    /// </summary>
    public decimal UsdRsdChangeThreshold { get; set; } = 1.0m;
}
