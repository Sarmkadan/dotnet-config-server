#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace DotnetConfigServer.Tests;

sealed public class WebhookServiceTests
{
    private readonly Mock<IWebhookSubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<IWebhookDeliveryRepository> _deliveryRepositoryMock;
    private readonly Mock<ILogger<WebhookService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly WebhookService _sut;

    public WebhookServiceTests()
    {
        _subscriptionRepositoryMock = new Mock<IWebhookSubscriptionRepository>();
        _deliveryRepositoryMock = new Mock<IWebhookDeliveryRepository>();
        _loggerMock = new Mock<ILogger<WebhookService>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpHandlerMock.Object);

        _sut = new WebhookService(
            _subscriptionRepositoryMock.Object,
            _deliveryRepositoryMock.Object,
            _loggerMock.Object,
            httpClient);
    }

    private static WebhookSubscription CreateValidSubscription() => new()
    {
        Name = "config-watcher",
        Url = "https://example.com/webhook",
        ConfigurationId = Guid.NewGuid(),
        CreatedBy = "admin",
        VerifySignature = false
    };

    [Fact]
    public async Task CreateSubscriptionAsync_WithValidSubscription_SavesAndReturns()
    {
        var subscription = CreateValidSubscription();
        var userId = "deploy-user";

        _subscriptionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<WebhookSubscription>())).Returns(Task.CompletedTask);
        _subscriptionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateSubscriptionAsync(subscription, userId);

        result.Should().NotBeNull();
        result.CreatedBy.Should().Be(userId);
        _subscriptionRepositoryMock.Verify(r => r.AddAsync(subscription), Times.Once);
        _subscriptionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithVerifySignatureAndNoSecret_GeneratesSecret()
    {
        var subscription = CreateValidSubscription();
        subscription.VerifySignature = true;
        subscription.Secret = null;

        _subscriptionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<WebhookSubscription>())).Returns(Task.CompletedTask);
        _subscriptionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.CreateSubscriptionAsync(subscription, "admin");

        subscription.Secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithInvalidUrl_ThrowsValidationException()
    {
        var subscription = CreateValidSubscription();
        subscription.Url = "not-a-valid-url";

        var act = () => _sut.CreateSubscriptionAsync(subscription, "admin");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetSubscriptionAsync_ExistingId_ReturnsSubscription()
    {
        var id = Guid.NewGuid();
        var subscription = CreateValidSubscription();
        subscription.Id = id;

        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(subscription);

        var result = await _sut.GetSubscriptionAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetSubscriptionAsync_NonExistentId_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((WebhookSubscription?)null);

        var result = await _sut.GetSubscriptionAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WithNonExistentId_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((WebhookSubscription?)null);

        var act = () => _sut.UpdateSubscriptionAsync(id, CreateValidSubscription(), "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WithValidData_UpdatesFields()
    {
        var id = Guid.NewGuid();
        var existing = CreateValidSubscription();
        existing.Id = id;
        var updated = new WebhookSubscription
        {
            Name = "new-name",
            Url = "https://newhost.example.com/hook",
            Description = "Updated description"
        };

        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _subscriptionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>())).Returns(Task.CompletedTask);
        _subscriptionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.UpdateSubscriptionAsync(id, updated, "editor");

        result.Name.Should().Be("new-name");
        result.Url.Should().Be("https://newhost.example.com/hook");
        result.UpdatedBy.Should().Be("editor");
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_WithNonExistentId_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((WebhookSubscription?)null);

        var act = () => _sut.DeleteSubscriptionAsync(id, "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_WithValidId_SetsIsActiveFalse()
    {
        var id = Guid.NewGuid();
        var subscription = CreateValidSubscription();
        subscription.Id = id;

        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(subscription);
        _subscriptionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>())).Returns(Task.CompletedTask);
        _subscriptionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.DeleteSubscriptionAsync(id, "admin");

        subscription.IsActive.Should().BeFalse();
        _subscriptionRepositoryMock.Verify(r => r.UpdateAsync(subscription), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WithNonExistentId_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((WebhookSubscription?)null);

        var act = () => _sut.ActivateAsync(id, "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    [Fact]
    public async Task ActivateAsync_WithDeactivatedSubscription_SetsIsActiveTrue()
    {
        var id = Guid.NewGuid();
        var subscription = CreateValidSubscription();
        subscription.Id = id;
        subscription.IsActive = false;
        subscription.Status = WebhookStatus.Failed;

        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(subscription);
        _subscriptionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>())).Returns(Task.CompletedTask);
        _subscriptionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ActivateAsync(id, "admin");

        result.IsActive.Should().BeTrue();
        result.Status.Should().Be(WebhookStatus.Active);
    }

    [Fact]
    public async Task DeactivateAsync_WithActiveSubscription_SetsIsActiveFalse()
    {
        var id = Guid.NewGuid();
        var subscription = CreateValidSubscription();
        subscription.Id = id;
        subscription.IsActive = true;

        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(subscription);
        _subscriptionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WebhookSubscription>())).Returns(Task.CompletedTask);
        _subscriptionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.DeactivateAsync(id, "admin");

        result.IsActive.Should().BeFalse();
        result.Status.Should().Be(WebhookStatus.Failed);
    }

    [Fact]
    public async Task GetDeliveriesAsync_ReturnsDeliveriesForSubscription()
    {
        var subscriptionId = Guid.NewGuid();
        var deliveries = new List<WebhookDelivery>
        {
            new() { WebhookSubscriptionId = subscriptionId, Payload = "{}", Status = WebhookDeliveryStatus.Success },
            new() { WebhookSubscriptionId = subscriptionId, Payload = "{}", Status = WebhookDeliveryStatus.Failed }
        };

        _deliveryRepositoryMock.Setup(r => r.GetBySubscriptionAsync(subscriptionId)).ReturnsAsync(deliveries);

        var result = await _sut.GetDeliveriesAsync(subscriptionId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RetryFailedDeliveriesAsync_SubscriptionNotActive_SkipsDelivery()
    {
        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = Guid.NewGuid(),
            Payload = "{}",
            AttemptNumber = 1,
            Status = WebhookDeliveryStatus.Failed
        };

        _deliveryRepositoryMock.Setup(r => r.GetFailedDeliveriesAsync()).ReturnsAsync(new List<WebhookDelivery> { delivery });
        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(delivery.WebhookSubscriptionId))
            .ReturnsAsync((WebhookSubscription?)null);
        _deliveryRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var count = await _sut.RetryFailedDeliveriesAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task RetryFailedDeliveriesAsync_DeliveryExceedsMaxRetries_IsSkipped()
    {
        var subscriptionId = Guid.NewGuid();
        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = subscriptionId,
            Payload = "{}",
            AttemptNumber = 10,
            Status = WebhookDeliveryStatus.Failed
        };
        var subscription = CreateValidSubscription();
        subscription.Id = subscriptionId;
        subscription.IsActive = true;

        _deliveryRepositoryMock.Setup(r => r.GetFailedDeliveriesAsync()).ReturnsAsync(new List<WebhookDelivery> { delivery });
        _subscriptionRepositoryMock.Setup(r => r.GetByIdAsync(subscriptionId)).ReturnsAsync(subscription);
        _deliveryRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var count = await _sut.RetryFailedDeliveriesAsync(maxRetries: 5);

        count.Should().Be(0);
    }

    [Fact]
    public void WebhookSubscription_GenerateSignature_WithNoSecret_ThrowsConfigurationException()
    {
        var subscription = CreateValidSubscription();
        subscription.Secret = null;

        var act = () => subscription.GenerateSignature("payload");

        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void WebhookSubscription_GenerateSignature_WithSecret_ReturnsHexString()
    {
        var subscription = CreateValidSubscription();
        subscription.Secret = "my-secret";

        var signature = subscription.GenerateSignature("test-payload");

        signature.Should().NotBeNullOrEmpty();
        signature.Should().MatchRegex("^[0-9A-F]+$");
    }

    [Fact]
    public void WebhookSubscription_IncrementRetryCount_ReachingMaxRetries_DeactivatesSubscription()
    {
        var subscription = CreateValidSubscription();
        subscription.MaxRetries = 3;

        subscription.IncrementRetryCount(503);
        subscription.IncrementRetryCount(503);
        subscription.IncrementRetryCount(503);

        subscription.RetryCount.Should().Be(3);
        subscription.IsActive.Should().BeFalse();
        subscription.Status.Should().Be(WebhookStatus.Failed);
    }

    [Fact]
    public void WebhookSubscription_ResetRetryCount_ClearsCounterAndRestoresActive()
    {
        var subscription = CreateValidSubscription();
        subscription.RetryCount = 5;
        subscription.Status = WebhookStatus.Failed;

        subscription.ResetRetryCount(200);

        subscription.RetryCount.Should().Be(0);
        subscription.Status.Should().Be(WebhookStatus.Active);
        subscription.LastDeliveryStatusCode.Should().Be(200);
    }
}
