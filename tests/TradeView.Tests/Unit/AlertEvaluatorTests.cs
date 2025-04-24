using TradeView.Core.Domain;
using TradeView.Core.Services;

namespace TradeView.Tests.Unit;

public class AlertEvaluatorTests
{
    private static readonly DateTime Now = new(2024, 6, 3, 14, 0, 0, DateTimeKind.Utc);

    private static PriceAlert Alert(
        AlertCondition condition,
        decimal threshold,
        decimal? reference = 100m,
        bool repeating = false,
        int cooldownSeconds = 300) => new()
    {
        Id = 7,
        InstrumentId = 1,
        Condition = condition,
        Threshold = threshold,
        ReferencePrice = reference,
        IsActive = true,
        IsRepeating = repeating,
        CooldownSeconds = cooldownSeconds,
        CreatedAtUtc = Now.AddHours(-1)
    };

    [Theory]
    [InlineData(149.99, false)]
    [InlineData(150.00, true)]
    [InlineData(151.25, true)]
    public void PriceAbove_fires_at_or_above_threshold(double price, bool expected)
    {
        var result = AlertEvaluator.Evaluate(Alert(AlertCondition.PriceAbove, 150m), "AAPL", (decimal)price, Now);

        Assert.Equal(expected, result.ShouldTrigger);
    }

    [Theory]
    [InlineData(90.01, false)]
    [InlineData(90.00, true)]
    [InlineData(85.00, true)]
    public void PriceBelow_fires_at_or_below_threshold(double price, bool expected)
    {
        var result = AlertEvaluator.Evaluate(Alert(AlertCondition.PriceBelow, 90m), "AAPL", (decimal)price, Now);

        Assert.Equal(expected, result.ShouldTrigger);
    }

    [Theory]
    [InlineData(104.99, false)]
    [InlineData(105.00, true)]
    [InlineData(96.00, false)]
    [InlineData(94.90, true)]
    public void PercentMove_fires_on_absolute_move_from_reference(double price, bool expected)
    {
        var result = AlertEvaluator.Evaluate(Alert(AlertCondition.PercentMove, 5m, reference: 100m), "AAPL", (decimal)price, Now);

        Assert.Equal(expected, result.ShouldTrigger);
    }

    [Fact]
    public void PercentMove_without_reference_never_fires()
    {
        var result = AlertEvaluator.Evaluate(Alert(AlertCondition.PercentMove, 1m, reference: null), "AAPL", 1000m, Now);

        Assert.False(result.ShouldTrigger);
    }

    [Fact]
    public void Inactive_alerts_never_fire()
    {
        var alert = Alert(AlertCondition.PriceAbove, 10m);
        alert.IsActive = false;

        Assert.False(AlertEvaluator.Evaluate(alert, "AAPL", 100m, Now).ShouldTrigger);
    }

    [Fact]
    public void Trigger_message_describes_the_breach()
    {
        var result = AlertEvaluator.Evaluate(Alert(AlertCondition.PriceAbove, 150m), "AAPL", 151.5m, Now);

        Assert.Equal("AAPL traded at 151.5, at or above 150", result.Message);
    }

    [Fact]
    public void Apply_deactivates_one_shot_alerts_and_returns_trigger()
    {
        var alert = Alert(AlertCondition.PriceAbove, 150m);

        var trigger = AlertEvaluator.Apply(alert, "AAPL", 155m, Now);

        Assert.NotNull(trigger);
        Assert.Equal(7, trigger!.PriceAlertId);
        Assert.Equal(155m, trigger.TriggeredPrice);
        Assert.Equal(Now, trigger.TriggeredAtUtc);
        Assert.False(alert.IsActive);
        Assert.Equal(Now, alert.LastTriggeredAtUtc);
    }

    [Fact]
    public void Apply_returns_null_and_leaves_state_untouched_when_not_hit()
    {
        var alert = Alert(AlertCondition.PriceAbove, 150m);

        Assert.Null(AlertEvaluator.Apply(alert, "AAPL", 120m, Now));
        Assert.True(alert.IsActive);
        Assert.Null(alert.LastTriggeredAtUtc);
    }

    [Fact]
    public void Repeating_alerts_stay_active_and_respect_cooldown()
    {
        var alert = Alert(AlertCondition.PriceAbove, 150m, repeating: true, cooldownSeconds: 300);

        Assert.NotNull(AlertEvaluator.Apply(alert, "AAPL", 155m, Now));
        Assert.True(alert.IsActive);

        // Still above threshold, but inside the cooldown window.
        Assert.Null(AlertEvaluator.Apply(alert, "AAPL", 156m, Now.AddSeconds(299)));

        // Cooldown elapsed.
        Assert.NotNull(AlertEvaluator.Apply(alert, "AAPL", 156m, Now.AddSeconds(300)));
        Assert.Equal(Now.AddSeconds(300), alert.LastTriggeredAtUtc);
    }

    [Fact]
    public void PercentMove_reports_signed_move()
    {
        var alert = Alert(AlertCondition.PercentMove, 1m, reference: 200m);

        Assert.Equal(-10m, AlertEvaluator.PercentMove(alert, 180m));
        Assert.Equal(2.5m, AlertEvaluator.PercentMove(alert, 205m));
    }
}
