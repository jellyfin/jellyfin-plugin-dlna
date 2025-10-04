using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Eventing;

/// <summary>
/// Defines the <see cref="DlnaEventManager" />.
/// </summary>
public class DlnaEventManager : IDlnaEventManager
{
    private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaEventManager"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public DlnaEventManager(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Renews an event subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription id.</param>
    /// <param name="notificationType">The notification type.</param>
    /// <param name="requestedTimeoutString">The requested timeout string.</param>
    /// <param name="callbackUrl">The callback URL.</param>
    /// <returns>The response to the renew subscription request.</returns>
    public EventSubscriptionResponse RenewEventSubscription(string? subscriptionId, string? notificationType, string? requestedTimeoutString, string? callbackUrl)
    {
        var subscription = GetSubscription(subscriptionId, false);
        if (subscription is not null && subscriptionId is not null)
        {
            subscription.TimeoutSeconds = ParseTimeout(requestedTimeoutString) ?? 300;
            int timeoutSeconds = subscription.TimeoutSeconds;
            subscription.SubscriptionTime = DateTime.UtcNow;

            _logger.LogDebug(
                "Renewing event subscription for {0} with timeout of {1} to {2}",
                subscription.NotificationType,
                timeoutSeconds,
                subscription.CallbackUrl);

            return GetEventSubscriptionResponse(subscriptionId, requestedTimeoutString, timeoutSeconds);
        }

        return new EventSubscriptionResponse();
    }

    /// <summary>
    /// Creates an event subscription.
    /// </summary>
    /// <param name="notificationType">The notification type.</param>
    /// <param name="requestedTimeoutString">The requested timeout string.</param>
    /// <param name="callbackUrl">The callback URL.</param>
    /// <returns>The response to the subscription request.</returns>
    public EventSubscriptionResponse CreateEventSubscription(string? notificationType, string? requestedTimeoutString, string? callbackUrl)
    {
        var timeout = ParseTimeout(requestedTimeoutString) ?? 300;
        var id = "uuid:" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        _logger.LogDebug(
            "Creating event subscription for {0} with timeout of {1} to {2}",
            notificationType,
            timeout,
            callbackUrl);

        _subscriptions.TryAdd(id, new EventSubscription
        {
            Id = id,
            CallbackUrl = callbackUrl,
            SubscriptionTime = DateTime.UtcNow,
            TimeoutSeconds = timeout,
            NotificationType = notificationType
        });

        return GetEventSubscriptionResponse(id, requestedTimeoutString, timeout);
    }

    private static int? ParseTimeout(string? header)
    {
        if (!string.IsNullOrEmpty(header))
        {
            // Starts with SECOND-
            if (int.TryParse(header.AsSpan().RightPart('-'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
            {
                return val;
            }
        }

        return null;
    }

    /// <summary>
    /// Cancels the event subscription of an subscriptionId.
    /// </summary>
    /// <param name="subscriptionId">The subscription id.</param>
    /// <returns>The response to the subscription cancellation request.</returns>
    public EventSubscriptionResponse CancelEventSubscription(string? subscriptionId)
    {
        _logger.LogDebug("Cancelling event subscription {0}", subscriptionId);

        if (subscriptionId is not null)
        {
            _subscriptions.TryRemove(subscriptionId, out _);
        }

        return new EventSubscriptionResponse();
    }

    private static EventSubscriptionResponse GetEventSubscriptionResponse(string subscriptionId, string? requestedTimeoutString, int timeoutSeconds)
    {
        var response = new EventSubscriptionResponse
        {
            Headers =
            {
                ["SID"] = subscriptionId,
                ["TIMEOUT"] = string.IsNullOrEmpty(requestedTimeoutString)
                    ? ("SECOND-" + timeoutSeconds.ToString(CultureInfo.InvariantCulture))
                    : requestedTimeoutString
            }
        };

        return response;
    }

    private EventSubscription? GetSubscription(string? id, bool throwOnMissing)
    {
        if (id is null || !_subscriptions.TryGetValue(id, out var e) && throwOnMissing)
        {
            throw new ResourceNotFoundException("Event with Id " + id + " not found.");
        }

        return e;
    }
}
