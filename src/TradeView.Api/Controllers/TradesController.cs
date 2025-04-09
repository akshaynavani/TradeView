using Microsoft.AspNetCore.Mvc;
using TradeView.Api.Contracts;
using TradeView.Core.Services;

namespace TradeView.Api.Controllers;

[ApiController]
[Route("api/trades")]
[Produces("application/json")]
public sealed class TradesController : ControllerBase
{
    private readonly PortfolioService _portfolio;

    public TradesController(PortfolioService portfolio)
    {
        _portfolio = portfolio;
    }

    /// <summary>Lists trades, newest first, optionally filtered by symbol and execution window.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TradeResponse>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<TradeResponse>> List(
        [FromQuery] string? symbol,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var trades = await _portfolio.ListTradesAsync(symbol, from, to, cancellationToken);
        return trades.Select(TradeResponse.From);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<TradeResponse> Get(long id, CancellationToken cancellationToken)
        => TradeResponse.From(await _portfolio.GetTradeAsync(id, cancellationToken));

    /// <summary>Records a buy or sell fill. Sells larger than the open position are rejected.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TradeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradeResponse>> Record(RecordTradeRequest request, CancellationToken cancellationToken)
    {
        var trade = await _portfolio.RecordTradeAsync(
            request.Symbol,
            request.Side!.Value,
            request.Quantity,
            request.Price,
            request.Fees,
            request.ExecutedAtUtc,
            request.Notes,
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = trade.Id }, TradeResponse.From(trade));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _portfolio.DeleteTradeAsync(id, cancellationToken);
        return NoContent();
    }
}
