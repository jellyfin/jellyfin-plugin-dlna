using System.Net.Http;
using Jellyfin.Plugin.Dlna.Eventing;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Service;

/// <summary>
/// Defines the <see cref="BaseService" />.
/// </summary>
public class BaseService : IDlnaEventManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseControlHandler"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    protected BaseService(ILogger<BaseService> logger, IHttpClientFactory httpClientFactory)
    {
        Logger = logger;
        EventManager = new DlnaEventManager(logger, httpClientFactory);
    }

    /// <summary>
    /// Gets the <see cref="IDlnaEventManager"/> instance.
    /// </summary>
    protected IDlnaEventManager EventManager { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/> instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Cancels an event subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription id.</param>
    /// <returns>EventSubscriptionResponse.</returns>
    public EventSubscriptionResponse CancelEventSubscription(string? subscriptionId)
    {
        return EventManager.CancelEventSubscription(subscriptionId);
    }

    /// <summary>
    /// Renews an event subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription id.</param>
    /// <param name="notificationType">The notification type.</param>
    /// <param name="requestedTimeoutString">The requested timeout string.</param>
    /// <param name="callbackUrl">The callback URL.</param>
    /// <returns>EventSubscriptionResponse.</returns>
    public EventSubscriptionResponse RenewEventSubscription(string? subscriptionId, string? notificationType, string? requestedTimeoutString, string? callbackUrl)
    {
        return EventManager.RenewEventSubscription(subscriptionId, notificationType, requestedTimeoutString, callbackUrl);
    }

    /// <summary>
    /// Creates an event subscription.
    /// </summary>
    /// <param name="notificationType">The notification type.</param>
    /// <param name="requestedTimeoutString">The requested timeout string.</param>
    /// <param name="callbackUrl">The callback URL.</param>
    /// <returns>EventSubscriptionResponse.</returns>
    public EventSubscriptionResponse CreateEventSubscription(string? notificationType, string? requestedTimeoutString, string? callbackUrl)
    {
        return EventManager.CreateEventSubscription(notificationType, requestedTimeoutString, callbackUrl);
    }
}
