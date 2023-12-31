#nullable disable
#pragma warning disable CS1591

using System.Net.Http;
using Jellyfin.Plugin.Dlna.Eventing;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Service;

public class BaseService : IDlnaEventManager
{
    protected BaseService(ILogger<BaseService> logger, IHttpClientFactory httpClientFactory)
    {
        Logger = logger;
        EventManager = new DlnaEventManager(logger, httpClientFactory);
    }

    protected IDlnaEventManager EventManager { get; }

    protected ILogger Logger { get; }

    public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
    {
        return EventManager.CancelEventSubscription(subscriptionId);
    }

    public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string requestedTimeoutString, string callbackUrl)
    {
        return EventManager.RenewEventSubscription(subscriptionId, notificationType, requestedTimeoutString, callbackUrl);
    }

    public EventSubscriptionResponse CreateEventSubscription(string notificationType, string requestedTimeoutString, string callbackUrl)
    {
        return EventManager.CreateEventSubscription(notificationType, requestedTimeoutString, callbackUrl);
    }
}
