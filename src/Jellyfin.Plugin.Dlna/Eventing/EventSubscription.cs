using System;

namespace Jellyfin.Plugin.Dlna.Eventing;

/// <summary>
/// Defines the <see cref="EventSubscription" />.
/// </summary>
public class EventSubscription
{
    /// <summary>
    /// Gets the id.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets the callback URL.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets the notification type.
    /// </summary>
    public string? NotificationType { get; set; }

    /// <summary>
    /// Gets the subscription time.
    /// </summary>
    public DateTime SubscriptionTime { get; set; }

    /// <summary>
    /// Gets the timeout seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets the trigger count.
    /// </summary>
    public long TriggerCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is expired.
    /// </summary>
    /// <value><c>true</c> if this instance is expired; otherwise, <c>false</c>.</value>
    public bool IsExpired => SubscriptionTime.AddSeconds(TimeoutSeconds) >= DateTime.UtcNow;

    /// <summary>
    /// Increments the trigger count.
    /// </summary>
    public void IncrementTriggerCount()
    {
        if (TriggerCount == long.MaxValue)
        {
            TriggerCount = 0;
        }

        TriggerCount++;
    }
}
