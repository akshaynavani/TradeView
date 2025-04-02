namespace TradeView.Core.Domain;

public enum AssetClass
{
    Equity = 0,
    Crypto = 1,
    Fx = 2,
    Commodity = 3,
    Index = 4
}

public enum TradeSide
{
    Buy = 0,
    Sell = 1
}

public enum AlertCondition
{
    /// <summary>Fires when the price trades at or above the threshold.</summary>
    PriceAbove = 0,

    /// <summary>Fires when the price trades at or below the threshold.</summary>
    PriceBelow = 1,

    /// <summary>Fires when the absolute percent move from the reference price reaches the threshold.</summary>
    PercentMove = 2
}
