using TradeView.Core.Abstractions;

namespace TradeView.Tests.TestSupport;

public sealed class FixedClock : IClock
{
    public FixedClock(DateTime? start = null)
    {
        UtcNow = start ?? new DateTime(2024, 6, 3, 14, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; set; }

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
