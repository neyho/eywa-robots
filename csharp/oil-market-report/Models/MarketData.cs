namespace OilMarketReport.Models;

/// <summary>
/// Container for all collected market data
/// </summary>
public class MarketData
{
    /// <summary>
    /// When this data was collected
    /// </summary>
    public DateTime CollectionTime { get; set; }
    
    /// <summary>
    /// List of oil prices (Brent, WTI)
    /// </summary>
    public List<OilPrice> OilPrices { get; set; } = new();
    
    /// <summary>
    /// List of exchange rates (EUR/USD, USD/RSD)
    /// </summary>
    public List<ExchangeRate> ExchangeRates { get; set; } = new();
    
    /// <summary>
    /// List of triggered alerts
    /// </summary>
    public List<string> Alerts { get; set; } = new();
    
    /// <summary>
    /// Whether any alerts were triggered
    /// </summary>
    public bool HasAlerts => Alerts?.Any() ?? false;
    
    /// <summary>
    /// Get Brent price (convenience method)
    /// </summary>
    public OilPrice? Brent => OilPrices.FirstOrDefault(p => p.Product == "Brent");
    
    /// <summary>
    /// Get WTI price (convenience method)
    /// </summary>
    public OilPrice? WTI => OilPrices.FirstOrDefault(p => p.Product == "WTI");
    
    /// <summary>
    /// Get EUR/USD rate (convenience method)
    /// </summary>
    public ExchangeRate? EurUsd => ExchangeRates.FirstOrDefault(r => r.Pair == "EUR/USD");
    
    /// <summary>
    /// Get USD/RSD rate (convenience method)
    /// </summary>
    public ExchangeRate? UsdRsd => ExchangeRates.FirstOrDefault(r => r.Pair == "USD/RSD");
}
