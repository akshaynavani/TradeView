namespace TradeView.Core.Domain;

public class PriceTick
{
    public long Id { get; set; }

    public int InstrumentId { get; set; }

    public Instrument? Instrument { get; set; }

    public decimal Price { get; set; }

    public decimal Volume { get; set; }

    public DateTime TimestampUtc { get; set; }
}
