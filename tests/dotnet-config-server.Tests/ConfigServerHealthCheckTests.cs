#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for <see cref="ConfigServerHealthCheck"/> and <see cref="ConfigWatcherStatus"/>.
/// </summary>
public sealed class ConfigServerHealthCheckTests
{
    private readonly Mock<IConfigStore> _configStoreMock = new();
    private readonly Mock<IWebhookDeliveryRepository> _deliveryRepositoryMock = new();
    private readonly ConfigWatcherStatus _watcherStatus = new();

    public ConfigServerHealthCheckTests()
    {
        _configStoreMock.Setup(s => s.IsReachableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _deliveryRepositoryMock.Setup(r => r.GetMostRecentAsync()).ReturnsAsync((WebhookDelivery?)null);
        _watcherStatus.MarkStarted();
    }

    private ConfigServerHealthCheck CreateSut() =>
        new(_configStoreMock.Object, _watcherStatus, _deliveryRepositoryMock.Object);

    private static Task<HealthCheckResult> Check(ConfigServerHealthCheck sut) =>
        sut.CheckHealthAsync(new HealthCheckContext());

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenAllSignalsAreGood()
    {
        _watcherStatus.RecordReload();
        _deliveryRepositoryMock.Setup(r => r.GetMostRecentAsync()).ReturnsAsync(new WebhookDelivery
        {
            Status = WebhookDeliveryStatus.Success,
            StatusCode = 200
        });

        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["configRootReachable"].Should().Be(true);
        result.Data["watcherRunning"].Should().Be(true);
        result.Data["lastWebhookDeliveryStatus"].Should().Be("Success");
        result.Data["lastWebhookDeliveryStatusCode"].Should().Be(200);
        result.Data["lastReloadUtc"].Should().NotBe("never");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenConfigRootIsUnreachable()
    {
        _configStoreMock.Setup(s => s.IsReachableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["configRootReachable"].Should().Be(false);
        _deliveryRepositoryMock.Verify(r => r.GetMostRecentAsync(), Times.Never);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenWatcherIsNotRunning()
    {
        _watcherStatus.MarkStopped();

        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("watcher is not running");
        result.Data["lastReloadUtc"].Should().Be("never");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenLastWebhookDeliveryFailed()
    {
        _deliveryRepositoryMock.Setup(r => r.GetMostRecentAsync()).ReturnsAsync(new WebhookDelivery
        {
            Status = WebhookDeliveryStatus.Failed,
            StatusCode = 503
        });

        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Last webhook delivery failed");
        result.Data["lastWebhookDeliveryStatusCode"].Should().Be(503);
    }

    [Fact]
    public async Task CheckHealthAsync_ReportsNoDeliveries_WhenNoneRecorded()
    {
        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["lastWebhookDeliveryStatus"].Should().Be("none");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenDeliveryProbeThrows()
    {
        _deliveryRepositoryMock.Setup(r => r.GetMostRecentAsync()).ThrowsAsync(new InvalidOperationException("db down"));

        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenStoreProbeThrows()
    {
        _configStoreMock.Setup(s => s.IsReachableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await Check(CreateSut());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void ConfigWatcherStatus_RecordReload_SetsLastReloadTime()
    {
        var before = DateTime.UtcNow;

        _watcherStatus.RecordReload();

        _watcherStatus.LastReloadUtc.Should().NotBeNull();
        _watcherStatus.LastReloadUtc!.Value.Should().BeOnOrAfter(before.AddSeconds(-1));
    }

    [Fact]
    public void ConfigWatcherStatus_MarkStopped_ClearsRunningState()
    {
        _watcherStatus.MarkStopped();

        _watcherStatus.IsRunning.Should().BeFalse();
    }
}
