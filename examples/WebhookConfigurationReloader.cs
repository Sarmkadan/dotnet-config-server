// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Examples
{
    /// <summary>
    /// Demonstrates how to implement webhook-based configuration reloading.
    /// When configuration changes in the server, clients receive immediate notifications
    /// and can update their in-memory configuration without restarting.
    /// </summary>
    public class WebhookConfigurationReloader
    {
        private readonly ILogger<WebhookConfigurationReloader> _logger;
        private readonly string _webhookSecret;
        private Dictionary<string, string> _configuration;

        public WebhookConfigurationReloader(ILogger<WebhookConfigurationReloader> logger, string webhookSecret)
        {
            _logger = logger;
            _webhookSecret = webhookSecret;
            _configuration = new Dictionary<string, string>();
        }

        /// <summary>
        /// Validates the webhook signature to ensure the request came from Config Server.
        /// Uses HMAC-SHA256 verification.
        /// </summary>
        private bool VerifyWebhookSignature(string payload, string signature)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret)))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedSignature = Convert.ToBase64String(computedHash);
                return computedSignature == signature;
            }
        }

        /// <summary>
        /// Middleware endpoint that receives configuration change notifications.
        /// </summary>
        public async Task HandleWebhookAsync(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Post)
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            // Read webhook payload
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

            // Verify signature
            if (!context.Request.Headers.TryGetValue("X-Webhook-Signature", out var signatureHeader))
            {
                _logger.LogWarning("Webhook received without signature");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (!VerifyWebhookSignature(body, signatureHeader.ToString()))
            {
                _logger.LogWarning("Invalid webhook signature");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Parse and process webhook
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var webhook = JsonSerializer.Deserialize<WebhookPayload>(body, options);

                await ProcessConfigurationChangeAsync(webhook);

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(new { status = "processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }

        /// <summary>
        /// Processes the configuration change event and reloads configuration.
        /// </summary>
        private async Task ProcessConfigurationChangeAsync(WebhookPayload webhook)
        {
            _logger.LogInformation(
                "Configuration changed: {ConfigurationId}, Type: {EventType}",
                webhook.ConfigurationId,
                webhook.EventType);

            // In a real application, fetch the updated configuration from the server
            // var updatedConfig = await _configClient.GetConfigurationAsync(webhook.ConfigurationId);

            // Apply changes to in-memory configuration
            foreach (var change in webhook.Changes ?? new List<ConfigurationChange>())
            {
                _logger.LogInformation(
                    "Applying change: {Key} = {Value}",
                    change.Key,
                    change.NewValue ?? "null");

                if (change.NewValue != null)
                    _configuration[change.Key] = change.NewValue;
                else
                    _configuration.Remove(change.Key);
            }

            _logger.LogInformation(
                "Configuration reloaded successfully. Total keys: {KeyCount}",
                _configuration.Count);

            // Trigger any necessary application updates
            await OnConfigurationReloadedAsync();
        }

        /// <summary>
        /// Called after configuration is reloaded. Override to trigger application-specific actions.
        /// </summary>
        protected virtual async Task OnConfigurationReloadedAsync()
        {
            // Example: Reconnect to database with new connection string
            // var newConnectionString = _configuration["Database:ConnectionString"];
            // await _dbContext.ReconnectAsync(newConnectionString);

            await Task.CompletedTask;
        }

        public string GetConfigurationValue(string key) =>
            _configuration.TryGetValue(key, out var value) ? value : null;

        public void SetInitialConfiguration(Dictionary<string, string> configuration)
        {
            _configuration = new Dictionary<string, string>(configuration);
            _logger.LogInformation("Initial configuration loaded with {KeyCount} keys", configuration.Count);
        }
    }

    public class WebhookPayload
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public string ConfigurationId { get; set; }
        public string ApplicationId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ConfigurationChange> Changes { get; set; }
    }

    public class ConfigurationChange
    {
        public string Key { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    // ASP.NET Core startup example
    public class WebhookStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<WebhookConfigurationReloader>();
        }

        public void Configure(IApplicationBuilder app, WebhookConfigurationReloader reloader)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/config/webhook", reloader.HandleWebhookAsync);
            });
        }
    }

    // Usage in dependency injection container
    public static class WebhookExtensions
    {
        public static IServiceCollection AddWebhookConfigurationReloader(this IServiceCollection services, string webhookSecret)
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WebhookConfigurationReloader>>();
                return new WebhookConfigurationReloader(logger, webhookSecret);
            });
            return services;
        }
    }
}
