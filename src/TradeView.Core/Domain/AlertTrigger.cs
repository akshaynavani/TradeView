namespace TradeView.Core.Domain;

public class AlertTrigger
{
    public long Id { get; set; }

    public int PriceAlertId { get; set; }

    public PriceAlert? PriceAlert { get; set; }

    public int InstrumentId { get; set; }

    public decimal TriggeredPrice { get; set; }

    public DateTime TriggeredAtUtc { get; set; }

    public string Message { get; set; } = string.Empty;
}
