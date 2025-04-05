using TradeView.Core.Domain;

namespace TradeView.Core.Abstractions;

public interface IPriceTickRepository
{
    Task AddRangeAsync(IEnumerable<PriceTick> ticks, CancellationToken cancellationToken = default);

    Task<PriceTick?> GetLatestAsync(int instrumentId, CancellationToken cancellationToken = default);

    /// <summary>Latest tick for each of the given instruments, keyed by instrument id.</summary>
    Task<IReadOnlyDictionary<int, PriceTick>> GetLatestAsync(
        IReadOnlyCollection<int> instrumentIds,
        CancellationToken cancellationToken = default);

    /// <summary>Ticks within [fromUtc, toUtc], oldest first, capped at <paramref name="limit"/> most recent.</summary>
    Task<IReadOnlyList<PriceTick>> GetHistoryAsync(
        int instrumentId,
        DateTime fromUtc,
        DateTime toUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
