#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Base exception for not found scenarios
/// </summary>
sealed public class NotFoundException : DotnetConfigServerException
{
    public NotFoundException(string message, string errorCode, object? details = null) : base(message, errorCode, details)
    {
    }
}

/// <summary>
/// Thrown when a requested application is not found
/// </summary>
sealed public class ApplicationNotFoundException : NotFoundException
{
    public ApplicationNotFoundException(string applicationId) : base($"Application '{applicationId}' not found", "APP_NOT_FOUND", new { ApplicationId = applicationId })
    {
    }

    public ApplicationNotFoundException(Guid applicationId) : base($"Application '{applicationId}' not found", "APP_NOT_FOUND", new { ApplicationId = applicationId })
    {
    }
}

/// <summary>
/// Thrown when a requested user is not found
/// </summary>
sealed public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(string userId) : base($"User '{userId}' not found", "USER_NOT_FOUND", new { UserId = userId })
    {
    }

    public UserNotFoundException(Guid userId) : base($"User '{userId}' not found", "USER_NOT_FOUND", new { UserId = userId })
    {
    }
}

/// <summary>
/// Thrown when a requested webhook delivery is not found
/// </summary>
sealed public class WebhookDeliveryNotFoundException : NotFoundException
{
    public WebhookDeliveryNotFoundException(string deliveryId) : base($"Webhook delivery '{deliveryId}' not found", "DELIVERY_NOT_FOUND", new { DeliveryId = deliveryId })
    {
    }

    public WebhookDeliveryNotFoundException(Guid deliveryId) : base($"Webhook delivery '{deliveryId}' not found", "DELIVERY_NOT_FOUND", new { DeliveryId = deliveryId })
    {
    }
}

/// <summary>
/// Thrown when a requested change request is not found
/// </summary>
sealed public class ChangeRequestNotFoundException : NotFoundException
{
    public ChangeRequestNotFoundException(string changeRequestId) : base($"Change request '{changeRequestId}' not found", "CHANGE_REQUEST_NOT_FOUND", new { ChangeRequestId = changeRequestId })
    {
    }

    public ChangeRequestNotFoundException(Guid changeRequestId) : base($"Change request '{changeRequestId}' not found", "CHANGE_REQUEST_NOT_FOUND", new { ChangeRequestId = changeRequestId })
    {
    }
}