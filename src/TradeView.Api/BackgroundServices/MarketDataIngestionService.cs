using Microsoft.Extensions.Options;
using TradeView.Core.Services;
using TradeView.Infrastructure.MarketData;

namespace TradeView.Api.BackgroundServices;

/// <summary>
/// Polls the market data feed on a fixed interval, persists ticks, evaluates price alerts against
/// the new prices, and periodically prunes old history.
/// Each cycle runs in its own DI scope so it gets a fresh DbContext.
/// </summary>
public sealed class MarketDataIngestionService : BackgroundService
{
    private static readonly TimeSpan PruneInterval = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<MarketDataOptions> _options;
    private readonly ILogger<MarketDataIngestionService> _logger;
    private DateTime _lastPruneUtc = DateTime.MinValue;

    public MarketDataIngestionService(
        IServiceScopeFactory scopeFactory,
        IOptions<MarketDataOptions> options,
        ILogger<MarketDataIngestionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.IngestionEnabled)
        {
            _logger.LogInformation("Market data ingestion is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds));
        _logger.LogInformation("Market data ingestion started, polling every {Interval}", interval);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await RunCycleAsync(options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A failed cycle must not kill the host; the next tick retries.
                _logger.LogError(ex, "Market data ingestion cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task RunCycleAsync(MarketDataOptions options, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var marketData = scope.ServiceProvider.GetRequiredService<MarketDataService>();

        var alerts = scope.ServiceProvider.GetRequiredService<AlertService>();

        var ticks = await marketData.IngestAsync(cancellationToken);
        _logger.LogDebug("Ingested {Count} ticks", ticks.Count);

        var triggers = await alerts.EvaluateAsync(ticks, cancellationToken);
        if (triggers.Count > 0)
        {
            _logger.LogInformation("{Count} price alert(s) fired", triggers.Count);
        }

        if (options.TickRetentionHours > 0 && DateTime.UtcNow - _lastPruneUtc > PruneInterval)
        {
            var removed = await marketData.PruneAsync(TimeSpan.FromHours(options.TickRetentionHours), cancellationToken);
            _lastPruneUtc = DateTime.UtcNow;
            if (removed > 0)
            {
                _logger.LogInformation("Pruned {Count} ticks older than {Hours}h", removed, options.TickRetentionHours);
            }
        }
    }
}
