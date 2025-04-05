using TradeView.Core.Domain;

namespace TradeView.Core.Abstractions;

public interface IInstrumentRepository
{
    Task<IReadOnlyList<Instrument>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<Instrument?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Instrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    Task<bool> SymbolExistsAsync(string symbol, CancellationToken cancellationToken = default);

    Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
