namespace TradeView.Core.Domain;

public class Trade
{
    public long Id { get; set; }

    public int InstrumentId { get; set; }

    public Instrument? Instrument { get; set; }

    public TradeSide Side { get; set; }

    /// <summary>Always positive; direction is carried by <see cref="Side"/>.</summary>
    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal Fees { get; set; }

    public DateTime ExecutedAtUtc { get; set; }

    public string? Notes { get; set; }
}
