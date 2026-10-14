#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.Metrics;
using DotnetConfigServer.Events;
using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Verifies the <c>WatchService</c> metrics published on the <see cref="ConfigServerTelemetry.MeterName"/> meter.
/// </summary>
public sealed class WatchServiceMetricsTests : IDisposable
{
    private const string ReloadsInstrument = "config_reloads_total";
    private const string DurationInstrument = "reload_duration_ms";

    private readonly MeterListener _listener = new();
    private readonly List<long> _reloadIncrements = new();
    private readonly List<double> _reloadDurations = new();
    private readonly object _lock = new();

    public WatchServiceMetricsTests()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ConfigServerTelemetry.MeterName &&
                (instrument.Name == ReloadsInstrument || instrument.Name == DurationInstrument))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name != ReloadsInstrument) return;
            lock (_lock) _reloadIncrements.Add(value);
        });

        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (instrument.Name != DurationInstrument) return;
            lock (_lock) _reloadDurations.Add(value);
        });

        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public async Task DispatchAsync_WithSubscriber_IncrementsReloadCounterAndRecordsDuration()
    {
        using var watchService = CreateWatchService();
        var configurationId = Guid.NewGuid();
        var received = 0;
        using var subscription = watchService.Watch(configurationId, _ =>
        {
            Interlocked.Increment(ref received);
            return Task.CompletedTask;
        });

        var reloadsBefore = ReloadTotal();

        await watchService.DispatchAsync(NewUpdatedEvent(configurationId));

        received.Should().Be(1);
        ReloadTotal().Should().Be(reloadsBefore + 1);

        lock (_lock)
        {
            _reloadDurations.Should().NotBeEmpty();
            _reloadDurations.Last().Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task DispatchAsync_WithoutSubscribers_DoesNotIncrementReloadCounter()
    {
        using var watchService = CreateWatchService();
        var reloadsBefore = ReloadTotal();

        await watchService.DispatchAsync(NewUpdatedEvent(Guid.NewGuid()));

        ReloadTotal().Should().Be(reloadsBefore);
    }

    private static WatchService CreateWatchService() =>
        new(
            new Mock<IEventBus>().Object,
            new Mock<IConfigurationService>().Object,
            new Mock<ILogger<WatchService>>().Object);

    private static DomainEvent NewUpdatedEvent(Guid configurationId)
    {
        // ConfigurationUpdatedEvent declares its own ConfigurationId, hiding DomainEvent.ConfigurationId.
        // WatchService routes on the base property, so set that one explicitly.
        DomainEvent domainEvent = new ConfigurationUpdatedEvent { ConfigurationName = "test" };
        domainEvent.ConfigurationId = configurationId;
        return domainEvent;
    }

    private long ReloadTotal()
    {
        lock (_lock) return _reloadIncrements.Sum();
    }
}
