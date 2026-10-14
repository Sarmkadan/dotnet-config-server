#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Shared metric instruments and log event ids for the change watcher (<c>WatchService</c>)
/// and webhook delivery (<c>WebhookNotifier</c>, <c>WebhookService</c>).
/// </summary>
/// <remarks>
/// Instruments are published on the <see cref="MeterName"/> meter:
/// <list type="bullet">
///   <item><description><c>config_reloads_total</c> (counter): change events dispatched to watch subscribers.</description></item>
///   <item><description><c>webhook_deliveries_total</c> (counter, tag <c>status</c> = <c>success</c> | <c>failure</c>): webhook HTTP deliveries.</description></item>
///   <item><description><c>reload_duration_ms</c> (histogram, unit ms): time taken to dispatch one change event to its subscribers.</description></item>
/// </list>
/// See the README "Observability" section for wiring these into OpenTelemetry.
/// </remarks>
public static class ConfigServerTelemetry
{
    /// <summary>
    /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/> that owns all config server instruments.
    /// Pass this to <c>AddMeter</c> in OpenTelemetry or to <c>MeterListener</c> to subscribe.
    /// </summary>
    public const string MeterName = "DotnetConfigServer";

    /// <summary>
    /// Tag key carried by <c>webhook_deliveries_total</c>.
    /// </summary>
    public const string StatusTagName = "status";

    /// <summary>
    /// Tag value for a delivery that received a 2xx response.
    /// </summary>
    public const string StatusSuccess = "success";

    /// <summary>
    /// Tag value for a delivery that returned a non-2xx status or failed with an exception.
    /// </summary>
    public const string StatusFailure = "failure";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    /// <summary>
    /// Counts change events dispatched to watch subscribers.
    /// </summary>
    public static readonly Counter<long> ConfigReloads =
        Meter.CreateCounter<long>("config_reloads_total", description: "Configuration change events dispatched to watch subscribers.");

    /// <summary>
    /// Counts webhook deliveries, tagged with <see cref="StatusTagName"/>.
    /// </summary>
    public static readonly Counter<long> WebhookDeliveries =
        Meter.CreateCounter<long>("webhook_deliveries_total", description: "Webhook deliveries, tagged by status (success or failure).");

    /// <summary>
    /// Increments <c>webhook_deliveries_total</c> for one delivery attempt.
    /// </summary>
    /// <param name="status">Either <see cref="StatusSuccess"/> or <see cref="StatusFailure"/>.</param>
    public static void RecordWebhookDelivery(string status) =>
        WebhookDeliveries.Add(1, new KeyValuePair<string, object?>(StatusTagName, status));

    /// <summary>
    /// Records how long a change event took to dispatch to its subscribers, in milliseconds.
    /// </summary>
    public static readonly Histogram<double> ReloadDurationMs =
        Meter.CreateHistogram<double>("reload_duration_ms", unit: "ms", description: "Time to dispatch a configuration change to watch subscribers.");

    /// <summary>
    /// Log event ids emitted by <c>WatchService</c>.
    /// </summary>
    public static class WatchEvents
    {
        /// <summary>A subscriber was registered with <c>WatchService.Watch</c>.</summary>
        public static readonly EventId Started = new(1001, "WatchStarted");

        /// <summary>A subscriber was removed or the watch service was disposed.</summary>
        public static readonly EventId Stopped = new(1002, "WatchStopped");

        /// <summary>A change event was dispatched to subscribers.</summary>
        public static readonly EventId Reloaded = new(1003, "ConfigReloaded");

        /// <summary>A subscriber callback threw while handling a change event.</summary>
        public static readonly EventId SubscriberFailed = new(1004, "WatchSubscriberFailed");
    }

    /// <summary>
    /// Log event ids emitted by <c>WebhookNotifier</c> and webhook delivery.
    /// </summary>
    public static class WebhookEvents
    {
        /// <summary>A domain event is being fanned out to webhook subscriptions.</summary>
        public static readonly EventId NotificationStarted = new(2001, "WebhookNotificationStarted");

        /// <summary>Fan-out of a domain event to webhook subscriptions failed before delivery.</summary>
        public static readonly EventId NotificationFailed = new(2002, "WebhookNotificationFailed");

        /// <summary>A webhook delivery returned a 2xx response.</summary>
        public static readonly EventId Sent = new(2010, "WebhookSent");

        /// <summary>A webhook delivery returned a non-2xx response or threw.</summary>
        public static readonly EventId Failed = new(2011, "WebhookFailed");
    }
}
