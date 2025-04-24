using TradeView.Core.Exceptions;
using TradeView.Core.Services;
using static TradeView.Tests.TestSupport.TradeBuilder;

namespace TradeView.Tests.Unit;

public class PnlCalculatorTests
{
    private const int InstrumentId = 1;
    private const string Symbol = "AAPL";

    [Fact]
    public void Single_buy_marks_unrealized_pnl_to_market()
    {
        var position = PnlCalculator.Calculate(InstrumentId, Symbol, new[] { Buy(10, 100m) }, marketPrice: 110m);

        Assert.Equal(10m, position.Quantity);
        Assert.Equal(100m, position.AverageCost);
        Assert.Equal(1000m, position.CostBasis);
        Assert.Equal(1100m, position.MarketValue);
        Assert.Equal(100m, position.UnrealizedPnl);
        Assert.Equal(0m, position.RealizedPnl);
        Assert.Equal(10m, position.UnrealizedPnlPercent);
    }

    [Fact]
    public void Multiple_buys_blend_into_average_cost()
    {
        var trades = new[] { Buy(10, 100m, minute: 0), Buy(10, 120m, minute: 1) };

        var position = PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: 115m);

        Assert.Equal(20m, position.Quantity);
        Assert.Equal(110m, position.AverageCost);
        Assert.Equal(100m, position.UnrealizedPnl);
    }

    [Fact]
    public void Buy_fees_are_capitalised_into_cost_basis()
    {
        var position = PnlCalculator.Calculate(InstrumentId, Symbol, new[] { Buy(10, 100m, fees: 5m) }, marketPrice: null);

        Assert.Equal(100.5m, position.AverageCost);
        Assert.Equal(5m, position.TotalFees);
    }

    [Fact]
    public void Partial_sell_realizes_gain_net_of_fees_and_keeps_average_cost()
    {
        var trades = new[] { Buy(10, 100m, minute: 0), Sell(4, 130m, fees: 2m, minute: 5) };

        var position = PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: 90m);

        Assert.Equal(6m, position.Quantity);
        Assert.Equal(100m, position.AverageCost);
        Assert.Equal(118m, position.RealizedPnl);   // (130 - 100) * 4 - 2
        Assert.Equal(-60m, position.UnrealizedPnl); // (90 - 100) * 6
        Assert.Equal(58m, position.TotalPnl);
    }

    [Fact]
    public void Closing_the_position_resets_average_cost()
    {
        var trades = new[] { Buy(5, 50m, minute: 0), Sell(5, 40m, minute: 1) };

        var position = PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: 60m);

        Assert.False(position.IsOpen);
        Assert.Equal(0m, position.AverageCost);
        Assert.Equal(-50m, position.RealizedPnl);
        Assert.Equal(0m, position.UnrealizedPnl);
        Assert.Equal(-50m, position.TotalPnl);
    }

    [Fact]
    public void Reopening_after_flat_starts_a_fresh_cost_basis()
    {
        var trades = new[] { Buy(5, 50m, minute: 0), Sell(5, 60m, minute: 1), Buy(2, 80m, minute: 2) };

        var position = PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: 80m);

        Assert.Equal(2m, position.Quantity);
        Assert.Equal(80m, position.AverageCost);
        Assert.Equal(50m, position.RealizedPnl);
        Assert.Equal(0m, position.UnrealizedPnl);
    }

    [Fact]
    public void Trades_are_replayed_in_execution_order_not_input_order()
    {
        var trades = new[] { Sell(5, 120m, minute: 10), Buy(10, 100m, minute: 0) };

        var position = PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: null);

        Assert.Equal(5m, position.Quantity);
        Assert.Equal(100m, position.RealizedPnl);
    }

    [Fact]
    public void Selling_more_than_held_is_rejected()
    {
        var trades = new[] { Buy(3, 100m, minute: 0), Sell(4, 100m, minute: 1) };

        Assert.Throws<DomainValidationException>(() =>
            PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: null));
    }

    [Fact]
    public void Without_market_price_unrealized_is_unknown_and_total_is_realized_only()
    {
        var trades = new[] { Buy(10, 100m, minute: 0), Sell(5, 110m, minute: 1) };

        var position = PnlCalculator.Calculate(InstrumentId, Symbol, trades, marketPrice: null);

        Assert.Null(position.MarketValue);
        Assert.Null(position.UnrealizedPnl);
        Assert.Equal(50m, position.TotalPnl);
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(-1, 100, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 100, -1)]
    public void Invalid_trade_values_are_rejected(int quantity, int price, int fees)
    {
        var trade = Buy(quantity, price, fees);

        Assert.Throws<DomainValidationException>(() =>
            PnlCalculator.Calculate(InstrumentId, Symbol, new[] { trade }, marketPrice: null));
    }

    [Fact]
    public void Trades_for_another_instrument_are_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            PnlCalculator.Calculate(InstrumentId, Symbol, new[] { Buy(1, 10m, instrumentId: 2) }, marketPrice: null));
    }

    [Fact]
    public void No_trades_yields_an_empty_position()
    {
        var position = PnlCalculator.Calculate(InstrumentId, Symbol, Array.Empty<TradeView.Core.Domain.Trade>(), 100m);

        Assert.Equal(0, position.TradeCount);
        Assert.Equal(0m, position.Quantity);
        Assert.Equal(0m, position.TotalPnl);
    }
}
