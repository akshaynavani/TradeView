using TradeView.Core.Domain;

namespace TradeView.Core.Abstractions;

public interface ITradeRepository
{
    Task<Trade?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Trades filtered by optional instrument and execution window, newest first.</summary>
    Task<IReadOnlyList<Trade>> QueryAsync(
        int? instrumentId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trade>> GetForInstrumentAsync(int instrumentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trade>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Trade trade, CancellationToken cancellationToken = default);

    void Remove(Trade trade);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
