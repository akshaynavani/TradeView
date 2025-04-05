namespace TradeView.Core.Abstractions;

/// <summary>Abstracts the system clock so time-dependent logic is testable.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
