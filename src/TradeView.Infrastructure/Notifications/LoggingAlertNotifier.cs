using Microsoft.Extensions.Logging;
using TradeView.Core.Abstractions;
using TradeView.Core.Domain;

namespace TradeView.Infrastructure.Notifications;

internal sealed class LoggingAlertNotifier : IAlertNotifier
{
    private readonly ILogger<LoggingAlertNotifier> _logger;

    public LoggingAlertNotifier(ILogger<LoggingAlertNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(IReadOnlyList<AlertTrigger> triggers, CancellationToken cancellationToken = default)
    {
        foreach (var trigger in triggers)
        {
            _logger.LogWarning("ALERT #{AlertId}: {Message}", trigger.PriceAlertId, trigger.Message);
        }

        return Task.CompletedTask;
    }
}
