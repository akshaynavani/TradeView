namespace TradeView.Core.Domain;

public class Instrument
{
    public int Id { get; set; }

    /// <summary>Ticker symbol, stored upper-case (e.g. AAPL, BTC-USD).</summary>
    public string Symbol { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public AssetClass AssetClass { get; set; }

    public string Currency { get; set; } = "USD";

    /// <summary>Starting price used by the simulated feed when no ticks exist yet.</summary>
    public decimal SeedPrice { get; set; }

    /// <summary>Only active instruments are polled by the ingestion service.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<PriceTick> Ticks { get; set; } = new List<PriceTick>();
}
