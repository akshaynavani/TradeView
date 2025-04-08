namespace TradeView.Infrastructure.MarketData;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    /// <summary>How often the ingestion service polls the feed.</summary>
    public int PollIntervalSeconds { get; set; } = 2;

    /// <summary>Per-tick standard deviation of the simulated log return, in percent.</summary>
    public double VolatilityPercent { get; set; } = 0.5;

    /// <summary>Per-tick drift of the simulated log return, in percent.</summary>
    public double DriftPercent { get; set; }

    /// <summary>Seed a default instrument universe on first start.</summary>
    public bool SeedInstruments { get; set; } = true;

    /// <summary>Ticks older than this are pruned. 0 disables pruning.</summary>
    public int TickRetentionHours { get; set; } = 24;

    /// <summary>Optional RNG seed for reproducible simulated prices.</summary>
    public int? RandomSeed { get; set; }

    /// <summary>Set to false to run the API without background ingestion.</summary>
    public bool IngestionEnabled { get; set; } = true;
}
