using Microsoft.EntityFrameworkCore;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Infrastructure.Persistence;

namespace TradeView.Infrastructure.Repositories;

internal sealed class AlertRepository : IAlertRepository
{
    private readonly TradeViewDbContext _db;

    public AlertRepository(TradeViewDbContext db)
    {
        _db = db;
    }

    public Task<PriceAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.PriceAlerts.Include(a => a.Instrument).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PriceAlert>> QueryAsync(int? instrumentId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _db.PriceAlerts.AsNoTracking().Include(a => a.Instrument).AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(a => a.InstrumentId == instrumentId.Value);
        }

        if (activeOnly)
        {
            query = query.Where(a => a.IsActive);
        }

        return await query.OrderBy(a => a.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceAlert>> GetActiveForInstrumentsAsync(
        IReadOnlyCollection<int> instrumentIds,
        CancellationToken cancellationToken = default)
        => await _db.PriceAlerts
            .Where(a => a.IsActive && instrumentIds.Contains(a.InstrumentId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AlertTrigger>> GetTriggersAsync(
        int? alertId,
        int? instrumentId,
        DateTime? sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AlertTriggers.AsNoTracking().Include(t => t.PriceAlert).ThenInclude(a => a!.Instrument).AsQueryable();

        if (alertId.HasValue)
        {
            query = query.Where(t => t.PriceAlertId == alertId.Value);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(t => t.InstrumentId == instrumentId.Value);
        }

        if (sinceUtc.HasValue)
        {
            query = query.Where(t => t.TriggeredAtUtc >= sinceUtc.Value);
        }

        return await query
            .OrderByDescending(t => t.TriggeredAtUtc)
            .ThenByDescending(t => t.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PriceAlert alert, CancellationToken cancellationToken = default)
        => await _db.PriceAlerts.AddAsync(alert, cancellationToken);

    public async Task AddTriggersAsync(IEnumerable<AlertTrigger> triggers, CancellationToken cancellationToken = default)
        => await _db.AlertTriggers.AddRangeAsync(triggers, cancellationToken);

    public void Remove(PriceAlert alert) => _db.PriceAlerts.Remove(alert);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
