using Microsoft.AspNetCore.Mvc;
using TradeView.Api.Contracts;
using TradeView.Core.Models;
using TradeView.Core.Services;

namespace TradeView.Api.Controllers;

[ApiController]
[Route("api/quotes")]
[Produces("application/json")]
public sealed class QuotesController : ControllerBase
{
    private readonly MarketDataService _marketData;

    public QuotesController(MarketDataService marketData)
    {
        _marketData = marketData;
    }

    /// <summary>Latest quote for every instrument that has price data.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<QuoteResponse>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<QuoteResponse>> Latest(CancellationToken cancellationToken)
    {
        var quotes = await _marketData.GetLatestQuotesAsync(cancellationToken);
        return quotes.Select(q => QuoteResponse.From(q.Instrument, q.Tick));
    }

    /// <summary>Latest quote for one instrument.</summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<QuoteResponse> Get(string symbol, CancellationToken cancellationToken)
    {
        var (instrument, tick) = await _marketData.GetLatestQuoteAsync(symbol, cancellationToken);
        return QuoteResponse.From(instrument, tick);
    }

    /// <summary>Tick history in a time window (defaults to the last hour), oldest first.</summary>
    [HttpGet("{symbol}/history")]
    [ProducesResponseType(typeof(PriceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PriceHistoryResponse> History(
        string symbol,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 500,
        CancellationToken cancellationToken = default)
    {
        var ticks = await _marketData.GetHistoryAsync(symbol, from, to, limit, cancellationToken);
        return new PriceHistoryResponse(
            InstrumentService.NormalizeSymbol(symbol),
            ticks.Count,
            ticks.Select(TickResponse.From).ToList());
    }

    /// <summary>OHLC, change, volume and VWAP over a time window (defaults to the last hour).</summary>
    [HttpGet("{symbol}/summary")]
    [ProducesResponseType(typeof(PriceSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PriceSummary> Summary(
        string symbol,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
        => _marketData.GetSummaryAsync(symbol, from, to, cancellationToken);
}
