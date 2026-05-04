// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents a webhook subscription for configuration change notifications
/// </summary>
public class WebhookSubscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Description { get; set; }

    [Required]
    public Guid ConfigurationId { get; set; }

    [Required]
    public WebhookStatus Status { get; set; } = WebhookStatus.Active;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastDeliveryAt { get; set; }

    public int LastDeliveryStatusCode { get; set; } = 0;

    [Required]
    public int RetryCount { get; set; } = 0;

    [Required]
    public int MaxRetries { get; set; } = AppConstants.Webhook.MaxRetries;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public string? UpdatedBy { get; set; }

    public string? Secret { get; set; }

    [Required]
    public bool VerifySignature { get; set; } = true;

    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// Validates the webhook subscription
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.AddError("Name", "Webhook name is required");

        if (Name?.Length > 256)
            errors.AddError("Name", "Webhook name cannot exceed 256 characters");

        if (string.IsNullOrWhiteSpace(Url))
            errors.AddError("Url", "Webhook URL is required");

        if (Url?.Length > AppConstants.Webhook.MaxWebhookUrl)
            errors.AddError("Url", $"Webhook URL cannot exceed {AppConstants.Webhook.MaxWebhookUrl} characters");

        if (!System.Text.RegularExpressions.Regex.IsMatch(Url ?? "", AppConstants.Validation.UrlPattern))
            errors.AddError("Url", "Invalid URL format. URL must start with http:// or https://");

        if (Description?.Length > 1024)
            errors.AddError("Description", "Description cannot exceed 1024 characters");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Webhook subscription validation failed", errors);
    }

    /// <summary>
    /// Marks webhook as failed and increments retry count
    /// </summary>
    public void IncrementRetryCount(int statusCode)
    {
        RetryCount++;
        LastDeliveryAt = DateTime.UtcNow;
        LastDeliveryStatusCode = statusCode;

        if (RetryCount >= MaxRetries)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Resets retry count on successful delivery
    /// </summary>
    public void ResetRetryCount(int statusCode)
    {
        RetryCount = 0;
        LastDeliveryAt = DateTime.UtcNow;
        LastDeliveryStatusCode = statusCode;
        Status = WebhookStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the webhook
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Status = WebhookStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the webhook
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Status = WebhookStatus.Active;
        RetryCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates HMAC SHA256 signature for webhook payload verification
    /// </summary>
    public string GenerateSignature(string payload)
    {
        if (string.IsNullOrEmpty(Secret))
            throw new Exceptions.ConfigurationException("Secret is not configured for signature generation");

        using (var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(Secret)))
        {
            var signature = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(signature);
        }
    }

    /// <summary>
    /// Gets a summary of this webhook subscription
    /// </summary>
    public WebhookSubscriptionSummary GetSummary()
    {
        return new WebhookSubscriptionSummary
        {
            Id = Id,
            Name = Name,
            Url = Url,
            Status = Status,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            LastDeliveryAt = LastDeliveryAt
        };
    }
}

/// <summary>
/// Summary view of a webhook subscription
/// </summary>
public class WebhookSubscriptionSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public WebhookStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
}
