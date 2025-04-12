using System.ComponentModel.DataAnnotations;
using TradeView.Core.Domain;

namespace TradeView.Api.Contracts;

public sealed class CreateAlertRequest
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    public AlertCondition? Condition { get; init; }

    /// <summary>Price level for PriceAbove/PriceBelow; percent (e.g. 2.5) for PercentMove.</summary>
    [Range(typeof(decimal), "0.00000001", "1000000000")]
    public decimal Threshold { get; init; }

    /// <summary>Keep firing (subject to cooldown) instead of deactivating after the first hit.</summary>
    public bool IsRepeating { get; init; }

    [Range(0, 604800)]
    public int CooldownSeconds { get; init; } = 300;

    [StringLength(500)]
    public string? Note { get; init; }
}

public sealed class UpdateAlertRequest
{
    [Range(typeof(decimal), "0.00000001", "1000000000")]
    public decimal? Threshold { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsRepeating { get; init; }

    [Range(0, 604800)]
    public int? CooldownSeconds { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }
}

public sealed record AlertResponse(
    int Id,
    string Symbol,
    AlertCondition Condition,
    decimal Threshold,
    decimal? ReferencePrice,
    bool IsActive,
    bool IsRepeating,
    int CooldownSeconds,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime? LastTriggeredAtUtc)
{
    public static AlertResponse From(PriceAlert a) => new(
        a.Id,
        a.Instrument?.Symbol ?? string.Empty,
        a.Condition,
        a.Threshold,
        a.ReferencePrice,
        a.IsActive,
        a.IsRepeating,
        a.CooldownSeconds,
        a.Note,
        a.CreatedAtUtc,
        a.LastTriggeredAtUtc);
}

public sealed record AlertTriggerResponse(
    long Id,
    int AlertId,
    string Symbol,
    AlertCondition? Condition,
    decimal TriggeredPrice,
    DateTime TriggeredAtUtc,
    string Message)
{
    public static AlertTriggerResponse From(AlertTrigger t) => new(
        t.Id,
        t.PriceAlertId,
        t.PriceAlert?.Instrument?.Symbol ?? string.Empty,
        t.PriceAlert?.Condition,
        t.TriggeredPrice,
        t.TriggeredAtUtc,
        t.Message);
}
