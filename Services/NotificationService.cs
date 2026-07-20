#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;

namespace DotnetConfigServer.Services;

/// <summary>
/// Interface for sending notifications.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(Notification notification);
    Task NotifyAsync(string type, object payload);
}

/// <summary>
/// Notification object.
/// </summary>
public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // info, warning, error
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Default implementation of INotificationService for sending notifications.
/// This service could integrate with external notification systems (e.g., email, SMS, push notifications).
/// For now, it logs notifications.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly TimeSpan _deduplicationWindow;
    private readonly ConcurrentDictionary<string, DeduplicationInfo> _deduplicationCache = new();

    /// <summary>
    /// Creates a new <see cref="NotificationService"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="deduplicationWindow">
    /// Optional deduplication window. Identical notifications (same Type and Message) within this window
    /// are suppressed and counted. Defaults to 60 seconds.
    /// </param>
    public NotificationService(
        ILogger<NotificationService> logger,
        TimeSpan? deduplicationWindow = null)
    {
        _logger = logger;
        _deduplicationWindow = deduplicationWindow ?? TimeSpan.FromSeconds(60);
    }

    public Task NotifyAsync(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Build a key that uniquely identifies a notification for deduplication.
        var key = $"{notification.Type}:{notification.Message}";
        var now = DateTime.UtcNow;

        // Try to get existing deduplication info.
        if (_deduplicationCache.TryGetValue(key, out var info))
        {
            // If we are still inside the deduplication window, increment the suppressed count and exit.
            if (now - info.FirstSent <= _deduplicationWindow)
            {
                Interlocked.Increment(ref info.SuppressedCount);
                return Task.CompletedTask;
            }

            // Window has elapsed – log any suppressed duplicates before sending the new notification.
            if (info.SuppressedCount > 0)
            {
                _logger.LogInformation(
                    "Suppressed {Count} duplicate notifications for Type '{Type}' with Message '{Message}' in the last {Window}s",
                    info.SuppressedCount,
                    notification.Type,
                    notification.Message,
                    _deduplicationWindow.TotalSeconds);
            }

            // Reset the deduplication entry for the new notification.
            _deduplicationCache[key] = new DeduplicationInfo { FirstSent = now, SuppressedCount = 0 };
        }
        else
        {
            // No prior entry – add a new one.
            _deduplicationCache[key] = new DeduplicationInfo { FirstSent = now, SuppressedCount = 0 };
        }

        // Log the notification (actual sending would be implemented here).
        _logger.LogInformation(
            "Sending notification (Type: {Type}, Severity: {Severity}, Message: {Message}, Metadata: {Metadata})",
            notification.Type,
            notification.Severity,
            notification.Message,
            JsonSerializer.Serialize(notification.Metadata));

        return Task.CompletedTask;
    }

    public Task NotifyAsync(string type, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(payload);

        // This overload does not participate in deduplication because it lacks a message field.
        _logger.LogInformation("Sending notification (Type: {Type}, Payload: {Payload})",
            type, JsonSerializer.Serialize(payload));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Internal structure used to track deduplication state.
    /// </summary>
    private sealed class DeduplicationInfo
    {
        public DateTime FirstSent;
        public int SuppressedCount;
    }
}
