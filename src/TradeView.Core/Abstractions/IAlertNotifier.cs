using TradeView.Core.Domain;

namespace TradeView.Core.Abstractions;

/// <summary>
/// Delivery channel for fired alerts. The default implementation logs; a webhook, email,
/// or SignalR notifier can be registered instead.
/// </summary>
public interface IAlertNotifier
{
    Task NotifyAsync(IReadOnlyList<AlertTrigger> triggers, CancellationToken cancellationToken = default);
}
