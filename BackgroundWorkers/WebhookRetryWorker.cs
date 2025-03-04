#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;

namespace DotnetConfigServer.BackgroundWorkers;

/// <summary>
/// Background worker that retries failed webhook deliveries.
/// Uses exponential backoff to avoid overwhelming external endpoints.
/// </summary>
sealed public class WebhookRetryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookRetryWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly int _maxRetries = 5;
    private readonly int _maxAgeHours = 24;

    public WebhookRetryWorker(IServiceProvider serviceProvider, ILogger<WebhookRetryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook retry worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RetryFailedDeliveriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during webhook retry");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Webhook retry worker stopped");
    }

    private async Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var deliveryRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryRepository>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

        try
        {
            // Get failed deliveries that haven't exceeded retry limit
            var failedDeliveries = await deliveryRepository.GetFailedDeliveriesAsync(_maxRetries, _maxAgeHours);

            if (failedDeliveries.Count == 0)
            {
                _logger.LogDebug("No failed webhook deliveries to retry");
                return;
            }

            _logger.LogInformation("Retrying {Count} failed webhook deliveries", failedDeliveries.Count);

            var retriedCount = 0;
            var successCount = 0;

            foreach (var delivery in failedDeliveries)
            {
                try
                {
                    var backoffDelay = GetExponentialBackoffDelay(delivery.RetryCount);
                    await Task.Delay(backoffDelay, cancellationToken);

                    var success = await webhookService.RetryWebhookDeliveryAsync(delivery.Id);

                    if (success)
                    {
                        successCount++;
                        _logger.LogInformation("Successfully retried webhook delivery {DeliveryId}", delivery.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Webhook delivery {DeliveryId} still failing", delivery.Id);
                    }

                    retriedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrying webhook delivery {DeliveryId}", delivery.Id);
                }
            }

            _logger.LogInformation("Webhook retry batch completed: {Retried} tried, {Success} succeeded", retriedCount, successCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed webhook deliveries");
            throw;
        }
    }

    /// <summary>
    /// Calculates exponential backoff delay to avoid hammering the endpoint.
    /// </summary>
    private int GetExponentialBackoffDelay(int retryCount)
    {
        const int baseDelay = 1000; // 1 second
        const int maxDelay = 300000; // 5 minutes

        var delay = baseDelay * (int)Math.Pow(2, retryCount);
        return Math.Min(delay, maxDelay);
    }
}

/// <summary>
/// Repository interface for webhook delivery operations.
/// </summary>
public interface IWebhookDeliveryRepository : IRepository<WebhookDelivery>
{
    Task<List<WebhookDelivery>> GetFailedDeliveriesAsync(int maxRetries, int maxAgeHours);
}
