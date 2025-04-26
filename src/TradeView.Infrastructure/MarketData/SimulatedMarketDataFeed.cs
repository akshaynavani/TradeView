using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;
using TradeView.Core.Models;

namespace TradeView.Infrastructure.MarketData;

/// <summary>
/// Generates prices with a geometric random walk: P(t+1) = P(t) * exp(mu + sigma * Z), Z ~ N(0,1).
/// Keeps per-instrument state in memory (registered as a singleton) and resumes from the last
/// persisted price after a restart.
/// </summary>
public sealed class SimulatedMarketDataFeed : IMarketDataFeed
{
    private readonly ConcurrentDictionary<int, decimal> _lastPrices = new();
    private readonly Random _random;
    private readonly object _randomLock = new();
    private readonly IClock _clock;
    private readonly double _sigma;
    private readonly double _mu;

    public SimulatedMarketDataFeed(IOptions<MarketDataOptions> options, IClock clock)
    {
        var value = options.Value;
        _clock = clock;
        _sigma = Math.Max(0, value.VolatilityPercent) / 100.0;
        _mu = value.DriftPercent / 100.0;
        _random = value.RandomSeed.HasValue ? new Random(value.RandomSeed.Value) : new Random();
    }

    public Task<IReadOnlyList<MarketQuote>> GetQuotesAsync(
        IReadOnlyList<Instrument> instruments,
        IReadOnlyDictionary<int, decimal> lastKnownPrices,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var quotes = new List<MarketQuote>(instruments.Count);

        foreach (var instrument in instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previous = _lastPrices.TryGetValue(instrument.Id, out var cached)
                ? cached
                : lastKnownPrices.TryGetValue(instrument.Id, out var persisted)
                    ? persisted
                    : instrument.SeedPrice;

            if (previous <= 0)
            {
                continue;
            }

            var (z, volumeNoise) = NextNoise();
            var next = (decimal)((double)previous * Math.Exp(_mu + _sigma * z));
            var decimals = DecimalsFor(instrument);
            var minimumTick = 1m / (decimal)Math.Pow(10, decimals);
            next = Math.Max(Math.Round(next, decimals, MidpointRounding.AwayFromZero), minimumTick);

            // Volume loosely scales with the size of the move.
            var volume = Math.Round((decimal)(100 + 900 * volumeNoise * (1 + Math.Abs(z))), 2);

            _lastPrices[instrument.Id] = next;
            quotes.Add(new MarketQuote(instrument.Id, instrument.Symbol, next, volume, now));
        }

        return Task.FromResult<IReadOnlyList<MarketQuote>>(quotes);
    }

    private (double Z, double Uniform) NextNoise()
    {
        lock (_randomLock)
        {
            // Box-Muller transform.
            var u1 = 1.0 - _random.NextDouble();
            var u2 = _random.NextDouble();
            var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return (z, _random.NextDouble());
        }
    }

    private static int DecimalsFor(Instrument instrument) => instrument.AssetClass switch
    {
        AssetClass.Fx => 5,
        AssetClass.Crypto => 4,
        _ => 2
    };
}
