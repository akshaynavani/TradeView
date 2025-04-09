using Microsoft.AspNetCore.Mvc;
using TradeView.Api.Contracts;
using TradeView.Core.Services;

namespace TradeView.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
[Produces("application/json")]
public sealed class PortfolioController : ControllerBase
{
    private readonly PortfolioService _portfolio;

    public PortfolioController(PortfolioService portfolio)
    {
        _portfolio = portfolio;
    }

    /// <summary>Portfolio-level realized and unrealized P&amp;L marked to the latest quotes.</summary>
    [HttpGet("pnl")]
    [ProducesResponseType(typeof(PortfolioResponse), StatusCodes.Status200OK)]
    public async Task<PortfolioResponse> GetPnl([FromQuery] bool includeClosed = false, CancellationToken cancellationToken = default)
        => PortfolioResponse.From(await _portfolio.GetPortfolioAsync(includeClosed, cancellationToken));

    /// <summary>Position and P&amp;L for a single instrument.</summary>
    [HttpGet("positions/{symbol}")]
    [ProducesResponseType(typeof(PositionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PositionResponse> GetPosition(string symbol, CancellationToken cancellationToken)
        => PositionResponse.From(await _portfolio.GetPositionAsync(symbol, cancellationToken));
}
