#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

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
sealed public class Notification
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
sealed public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(Notification notification)
    {
        _logger.LogInformation(
            "Sending notification (Type: {Type}, Severity: {Severity}, Message: {Message}, Metadata: {Metadata})",
            notification.Type, notification.Severity, notification.Message,
            JsonSerializer.Serialize(notification.Metadata));
        return Task.CompletedTask;
    }

    public Task NotifyAsync(string type, object payload)
    {
        _logger.LogInformation("Sending notification (Type: {Type}, Payload: {Payload})",
            type, JsonSerializer.Serialize(payload));
        return Task.CompletedTask;
    }
}
