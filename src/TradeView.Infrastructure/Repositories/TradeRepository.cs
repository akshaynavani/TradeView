using Microsoft.EntityFrameworkCore;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Infrastructure.Persistence;

namespace TradeView.Infrastructure.Repositories;

internal sealed class TradeRepository : ITradeRepository
{
    private readonly TradeViewDbContext _db;

    public TradeRepository(TradeViewDbContext db)
    {
        _db = db;
    }

    public Task<Trade?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _db.Trades.Include(t => t.Instrument).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Trade>> QueryAsync(
        int? instrumentId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Trades.AsNoTracking().Include(t => t.Instrument).AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(t => t.InstrumentId == instrumentId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(t => t.ExecutedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(t => t.ExecutedAtUtc <= toUtc.Value);
        }

        return await query
            .OrderByDescending(t => t.ExecutedAtUtc)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Trade>> GetForInstrumentAsync(int instrumentId, CancellationToken cancellationToken = default)
        => await _db.Trades
            .AsNoTracking()
            .Where(t => t.InstrumentId == instrumentId)
            .OrderBy(t => t.ExecutedAtUtc)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Trade>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Trades
            .AsNoTracking()
            .Include(t => t.Instrument)
            .OrderBy(t => t.ExecutedAtUtc)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Trade trade, CancellationToken cancellationToken = default)
        => await _db.Trades.AddAsync(trade, cancellationToken);

    public void Remove(Trade trade) => _db.Trades.Remove(trade);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
