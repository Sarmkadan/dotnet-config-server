#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Events;

namespace DotnetConfigServer.Services;

/// <summary>
/// In-memory implementation of the INotificationService.
/// This service logs notifications and stores them in memory.
/// </summary>
sealed public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly List<Notification> _notifications = new();

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(Notification notification)
    {
        _logger.LogInformation(
            "Notification (Type: {Type}, Severity: {Severity}): {Message}",
            notification.Type, notification.Severity, notification.Message);

        _notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task NotifyAsync(string type, object payload)
    {
        _logger.LogInformation("Notification (Type: {Type}): {Payload}", type, System.Text.Json.JsonSerializer.Serialize(payload));
        // For simplicity, not storing the object payload directly in the list, but it could be serialized.
        _notifications.Add(new Notification
        {
            Type = type,
            Message = $"Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}",
            Severity = "info",
            CreatedAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// For testing/debugging: get all notifications received.
    /// </summary>
    public IReadOnlyList<Notification> GetNotifications() => _notifications.AsReadOnly();
}
