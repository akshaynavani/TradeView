using System.Globalization;
using TradeView.Core.Domain;

namespace TradeView.Core.Services;

public sealed record AlertEvaluation(bool ShouldTrigger, string? Message)
{
    public static readonly AlertEvaluation None = new(false, null);
}

/// <summary>Pure alert rule evaluation. Has no I/O so every rule is directly unit testable.</summary>
public static class AlertEvaluator
{
    public static AlertEvaluation Evaluate(PriceAlert alert, string symbol, decimal price, DateTime nowUtc)
    {
        if (!alert.IsActive || price <= 0)
        {
            return AlertEvaluation.None;
        }

        if (IsCoolingDown(alert, nowUtc))
        {
            return AlertEvaluation.None;
        }

        return alert.Condition switch
        {
            AlertCondition.PriceAbove when price >= alert.Threshold =>
                Hit($"{symbol} traded at {Fmt(price)}, at or above {Fmt(alert.Threshold)}"),

            AlertCondition.PriceBelow when price <= alert.Threshold =>
                Hit($"{symbol} traded at {Fmt(price)}, at or below {Fmt(alert.Threshold)}"),

            AlertCondition.PercentMove when PercentMove(alert, price) is { } move && Math.Abs(move) >= alert.Threshold =>
                Hit($"{symbol} moved {Fmt(Math.Round(move, 2))}% from {Fmt(alert.ReferencePrice!.Value)} to {Fmt(price)} (threshold {Fmt(alert.Threshold)}%)"),

            _ => AlertEvaluation.None
        };
    }

    /// <summary>
    /// Evaluates the alert and, if it fires, mutates its state (last-triggered time, one-shot
    /// deactivation) and returns the trigger record to persist.
    /// </summary>
    public static AlertTrigger? Apply(PriceAlert alert, string symbol, decimal price, DateTime nowUtc)
    {
        var evaluation = Evaluate(alert, symbol, price, nowUtc);
        if (!evaluation.ShouldTrigger)
        {
            return null;
        }

        alert.LastTriggeredAtUtc = nowUtc;
        if (!alert.IsRepeating)
        {
            alert.IsActive = false;
        }

        return new AlertTrigger
        {
            PriceAlertId = alert.Id,
            PriceAlert = alert,
            InstrumentId = alert.InstrumentId,
            TriggeredPrice = price,
            TriggeredAtUtc = nowUtc,
            Message = evaluation.Message!
        };
    }

    /// <summary>Signed percent move from the reference price, or null when there is no reference.</summary>
    public static decimal? PercentMove(PriceAlert alert, decimal price)
        => alert.ReferencePrice is > 0 and var reference
            ? (price - reference.Value) / reference.Value * 100m
            : null;

    private static bool IsCoolingDown(PriceAlert alert, DateTime nowUtc)
        => alert.LastTriggeredAtUtc.HasValue
           && nowUtc - alert.LastTriggeredAtUtc.Value < TimeSpan.FromSeconds(Math.Max(0, alert.CooldownSeconds));

    private static AlertEvaluation Hit(string message) => new(true, message);

    private static string Fmt(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
}
