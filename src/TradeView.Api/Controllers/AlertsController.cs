using Microsoft.AspNetCore.Mvc;
using TradeView.Api.Contracts;
using TradeView.Core.Services;

namespace TradeView.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public sealed class AlertsController : ControllerBase
{
    private readonly AlertService _alerts;

    public AlertsController(AlertService alerts)
    {
        _alerts = alerts;
    }

    /// <summary>Lists configured price alerts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertResponse>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<AlertResponse>> List(
        [FromQuery] string? symbol,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var alerts = await _alerts.ListAsync(symbol, activeOnly, cancellationToken);
        return alerts.Select(AlertResponse.From);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AlertResponse> Get(int id, CancellationToken cancellationToken)
        => AlertResponse.From(await _alerts.GetAsync(id, cancellationToken));

    /// <summary>Creates a price alert. The current quote is captured as the reference price.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> Create(CreateAlertRequest request, CancellationToken cancellationToken)
    {
        var alert = await _alerts.CreateAsync(
            request.Symbol,
            request.Condition!.Value,
            request.Threshold,
            request.IsRepeating,
            request.CooldownSeconds,
            request.Note,
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = alert.Id }, AlertResponse.From(alert));
    }

    /// <summary>Changes threshold, cooldown or repeat mode, or (re)activates an alert.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AlertResponse> Update(int id, UpdateAlertRequest request, CancellationToken cancellationToken)
    {
        var alert = await _alerts.UpdateAsync(
            id,
            request.Threshold,
            request.IsActive,
            request.IsRepeating,
            request.CooldownSeconds,
            request.Note,
            cancellationToken);

        return AlertResponse.From(alert);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _alerts.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>History of fired alerts across all alerts, newest first.</summary>
    [HttpGet("triggers")]
    [ProducesResponseType(typeof(IEnumerable<AlertTriggerResponse>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<AlertTriggerResponse>> Triggers(
        [FromQuery] string? symbol,
        [FromQuery] DateTime? since,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var triggers = await _alerts.GetTriggersAsync(null, symbol, since, limit, cancellationToken);
        return triggers.Select(AlertTriggerResponse.From);
    }

    /// <summary>History of a single alert's triggers, newest first.</summary>
    [HttpGet("{id:int}/triggers")]
    [ProducesResponseType(typeof(IEnumerable<AlertTriggerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IEnumerable<AlertTriggerResponse>> TriggersForAlert(
        int id,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        await _alerts.GetAsync(id, cancellationToken);
        var triggers = await _alerts.GetTriggersAsync(id, null, null, limit, cancellationToken);
        return triggers.Select(AlertTriggerResponse.From);
    }
}
