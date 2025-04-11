using TradeView.Core.Domain;

namespace TradeView.Core.Abstractions;

public interface IAlertRepository
{
    Task<PriceAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceAlert>> QueryAsync(int? instrumentId, bool activeOnly, CancellationToken cancellationToken = default);

    /// <summary>Active alerts for the given instruments, tracked for update.</summary>
    Task<IReadOnlyList<PriceAlert>> GetActiveForInstrumentsAsync(
        IReadOnlyCollection<int> instrumentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertTrigger>> GetTriggersAsync(
        int? alertId,
        int? instrumentId,
        DateTime? sinceUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(PriceAlert alert, CancellationToken cancellationToken = default);

    Task AddTriggersAsync(IEnumerable<AlertTrigger> triggers, CancellationToken cancellationToken = default);

    void Remove(PriceAlert alert);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
