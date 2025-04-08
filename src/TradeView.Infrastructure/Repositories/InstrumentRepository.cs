using Microsoft.EntityFrameworkCore;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Infrastructure.Persistence;

namespace TradeView.Infrastructure.Repositories;

internal sealed class InstrumentRepository : IInstrumentRepository
{
    private readonly TradeViewDbContext _db;

    public InstrumentRepository(TradeViewDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Instrument>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _db.Instruments.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(i => i.IsActive);
        }

        return await query.OrderBy(i => i.Symbol).ToListAsync(cancellationToken);
    }

    public Task<Instrument?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Instruments.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Instrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
        => _db.Instruments.FirstOrDefaultAsync(i => i.Symbol == symbol, cancellationToken);

    public Task<bool> SymbolExistsAsync(string symbol, CancellationToken cancellationToken = default)
        => _db.Instruments.AnyAsync(i => i.Symbol == symbol, cancellationToken);

    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default)
        => await _db.Instruments.AddAsync(instrument, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
