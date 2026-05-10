// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Models;

/// <summary>
/// Represents a webhook delivery attempt
/// </summary>
public class WebhookDelivery
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid WebhookSubscriptionId { get; set; }

    [Required]
    public Guid ConfigurationVersionId { get; set; }

    [Required]
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [Required]
    public int AttemptNumber { get; set; } = 1;

    [Required]
    public int StatusCode { get; set; } = 0;

    public int? ResponseTimeMs { get; set; }

    public string? ResponseBody { get; set; }

    public string? ErrorMessage { get; set; }

    [Required]
    public string Payload { get; set; } = string.Empty;

    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Marks the delivery as successful
    /// </summary>
    public void MarkSuccess(int statusCode, int responseTimeMs, string? responseBody = null)
    {
        Status = WebhookDeliveryStatus.Success;
        StatusCode = statusCode;
        ResponseTimeMs = responseTimeMs;
        ResponseBody = responseBody;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the delivery as failed and schedules retry
    /// </summary>
    public void MarkFailed(string errorMessage, int statusCode = 0, int? responseTimeMs = null)
    {
        Status = WebhookDeliveryStatus.Failed;
        StatusCode = statusCode;
        ResponseTimeMs = responseTimeMs;
        ErrorMessage = errorMessage;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Schedules a retry attempt
    /// </summary>
    public void ScheduleRetry(int delaySeconds = 300)
    {
        Status = WebhookDeliveryStatus.Retry;
        AttemptNumber++;
        NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
    }

    /// <summary>
    /// Checks if the delivery should be retried based on attempt count
    /// </summary>
    public bool ShouldRetry(int maxRetries = 5)
    {
        return Status == WebhookDeliveryStatus.Failed && AttemptNumber < maxRetries;
    }

    /// <summary>
    /// Gets a summary of the delivery
    /// </summary>
    public WebhookDeliverySummary GetSummary()
    {
        return new WebhookDeliverySummary
        {
            Id = Id,
            Status = Status,
            AttemptNumber = AttemptNumber,
            StatusCode = StatusCode,
            CreatedAt = CreatedAt,
            SentAt = SentAt,
            ResponseTimeMs = ResponseTimeMs
        };
    }

    /// <summary>
    /// Validates the webhook delivery
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (WebhookSubscriptionId == Guid.Empty)
            errors.AddError("WebhookSubscriptionId", "Webhook subscription ID is required");

        if (ConfigurationVersionId == Guid.Empty)
            errors.AddError("ConfigurationVersionId", "Configuration version ID is required");

        if (string.IsNullOrWhiteSpace(Payload))
            errors.AddError("Payload", "Webhook payload is required");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Webhook delivery validation failed", errors);
    }
}

/// <summary>
/// Summary view of a webhook delivery
/// </summary>
public class WebhookDeliverySummary
{
    public Guid Id { get; set; }
    public WebhookDeliveryStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int? ResponseTimeMs { get; set; }
}
