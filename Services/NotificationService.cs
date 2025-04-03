#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotnetConfigServer.Events;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

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
