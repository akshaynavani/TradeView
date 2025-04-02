namespace TradeView.Core.Domain;

public class PriceAlert
{
    public int Id { get; set; }

    public int InstrumentId { get; set; }

    public Instrument? Instrument { get; set; }

    public AlertCondition Condition { get; set; }

    /// <summary>A price for PriceAbove/PriceBelow, a percentage (e.g. 5 = 5%) for PercentMove.</summary>
    public decimal Threshold { get; set; }

    /// <summary>Price captured when the alert was created; the base for PercentMove.</summary>
    public decimal? ReferencePrice { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When false the alert deactivates after its first trigger.</summary>
    public bool IsRepeating { get; set; }

    /// <summary>Minimum seconds between two triggers of a repeating alert.</summary>
    public int CooldownSeconds { get; set; } = 300;

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastTriggeredAtUtc { get; set; }

    public ICollection<AlertTrigger> Triggers { get; set; } = new List<AlertTrigger>();
}
