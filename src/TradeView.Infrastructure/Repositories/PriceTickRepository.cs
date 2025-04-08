using Microsoft.EntityFrameworkCore;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Infrastructure.Persistence;

namespace TradeView.Infrastructure.Repositories;

internal sealed class PriceTickRepository : IPriceTickRepository
{
    private readonly TradeViewDbContext _db;

    public PriceTickRepository(TradeViewDbContext db)
    {
        _db = db;
    }

    public async Task AddRangeAsync(IEnumerable<PriceTick> ticks, CancellationToken cancellationToken = default)
    {
        await _db.PriceTicks.AddRangeAsync(ticks, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PriceTick?> GetLatestAsync(int instrumentId, CancellationToken cancellationToken = default)
        => _db.PriceTicks
            .AsNoTracking()
            .Where(t => t.InstrumentId == instrumentId)
            .OrderByDescending(t => t.TimestampUtc)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, PriceTick>> GetLatestAsync(
        IReadOnlyCollection<int> instrumentIds,
        CancellationToken cancellationToken = default)
    {
        if (instrumentIds.Count == 0)
        {
            return new Dictionary<int, PriceTick>();
        }

        // Ticks are appended in time order, so the highest id per instrument is the latest tick.
        var latestIds = _db.PriceTicks
            .Where(t => instrumentIds.Contains(t.InstrumentId))
            .GroupBy(t => t.InstrumentId)
            .Select(g => g.Max(t => t.Id));

        var ticks = await _db.PriceTicks
            .AsNoTracking()
            .Where(t => latestIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        return ticks.ToDictionary(t => t.InstrumentId);
    }

    public async Task<IReadOnlyList<PriceTick>> GetHistoryAsync(
        int instrumentId,
        DateTime fromUtc,
        DateTime toUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var newestFirst = await _db.PriceTicks
            .AsNoTracking()
            .Where(t => t.InstrumentId == instrumentId && t.TimestampUtc >= fromUtc && t.TimestampUtc <= toUtc)
            .OrderByDescending(t => t.TimestampUtc)
            .ThenByDescending(t => t.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        newestFirst.Reverse();
        return newestFirst;
    }

    public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        => _db.PriceTicks
            .Where(t => t.TimestampUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
}
