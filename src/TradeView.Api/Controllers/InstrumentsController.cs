using Microsoft.AspNetCore.Mvc;
using TradeView.Api.Contracts;
using TradeView.Core.Services;

namespace TradeView.Api.Controllers;

[ApiController]
[Route("api/instruments")]
[Produces("application/json")]
public sealed class InstrumentsController : ControllerBase
{
    private readonly InstrumentService _instruments;

    public InstrumentsController(InstrumentService instruments)
    {
        _instruments = instruments;
    }

    /// <summary>Lists tracked instruments.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InstrumentResponse>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<InstrumentResponse>> List([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var instruments = await _instruments.ListAsync(activeOnly, cancellationToken);
        return instruments.Select(InstrumentResponse.From);
    }

    /// <summary>Gets a single instrument by symbol.</summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(InstrumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<InstrumentResponse> Get(string symbol, CancellationToken cancellationToken)
        => InstrumentResponse.From(await _instruments.GetRequiredAsync(symbol, cancellationToken));

    /// <summary>Starts tracking a new instrument.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InstrumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstrumentResponse>> Create(CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var instrument = await _instruments.CreateAsync(
            request.Symbol,
            request.Name ?? string.Empty,
            request.AssetClass,
            request.Currency,
            request.SeedPrice,
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { symbol = instrument.Symbol }, InstrumentResponse.From(instrument));
    }

    /// <summary>Renames an instrument or pauses/resumes its price ingestion.</summary>
    [HttpPatch("{symbol}")]
    [ProducesResponseType(typeof(InstrumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<InstrumentResponse> Update(string symbol, UpdateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var instrument = await _instruments.UpdateAsync(symbol, request.Name, request.IsActive, cancellationToken);
        return InstrumentResponse.From(instrument);
    }
}
