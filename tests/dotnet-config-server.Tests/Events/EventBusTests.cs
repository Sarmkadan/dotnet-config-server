#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotnetConfigServer.Tests.Events;

/// <summary>
/// Tests for EventBus class
/// </summary>
public class EventBusTests
{
    private readonly EventBus _eventBus;

    public EventBusTests()
    {
        _eventBus = new EventBus(new NullLogger<EventBus>());
    }

    private sealed class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }

    public class Constructor : EventBusTests
    {
        [Fact]
        public void CreatesInstanceWithLogger()
        {
            // Act
            var bus = new EventBus(null);

            // Assert
            bus.Should().NotBeNull();
        }
    }

    public class PublishAsync : EventBusTests
    {
        [Fact]
        public async Task PublishesEventToSingleHandler()
        {
            // Arrange
            var eventRaised = false;
            var testEvent = new TestDomainEvent();

            _eventBus.Subscribe<TestDomainEvent>(e => {
                eventRaised = true;
                return Task.CompletedTask;
            });

            // Act
            await _eventBus.PublishAsync(testEvent);

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public async Task PublishesEventToMultipleHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler3Called = false;
            var testEvent = new TestDomainEvent();

            _eventBus.Subscribe<TestDomainEvent>(e => {
                handler1Called = true;
                return Task.CompletedTask;
            });

            _eventBus.Subscribe<TestDomainEvent>(e => {
                handler2Called = true;
                return Task.CompletedTask;
            });

            _eventBus.Subscribe<TestDomainEvent>(e => {
                handler3Called = true;
                return Task.CompletedTask;
            });

            // Act
            await _eventBus.PublishAsync(testEvent);

            // Assert
            handler1Called.Should().BeTrue();
            handler2Called.Should().BeTrue();
            handler3Called.Should().BeTrue();
        }

        [Fact]
        public async Task PublishesEventToHandlersInParallel()
        {
            // Arrange
            var handler1Completed = new TaskCompletionSource<bool>();
            var handler2Completed = new TaskCompletionSource<bool>();
            var testEvent = new TestDomainEvent();

            _eventBus.Subscribe<TestDomainEvent>(async e => {
                await Task.Delay(100); // Simulate work
                handler1Completed.SetResult(true);
            });

            _eventBus.Subscribe<TestDomainEvent>(async e => {
                await Task.Delay(50); // Simulate work
                handler2Completed.SetResult(true);
            });

            // Act
            await _eventBus.PublishAsync(testEvent);

            // Assert - both handlers should complete
            (await handler1Completed.Task.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();
            (await handler2Completed.Task.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();
        }

        [Fact]
        public async Task DoesNotThrowWhenNoHandlersRegistered()
        {
            // Arrange
            var testEvent = new TestDomainEvent();

            // Act & Assert - should not throw
            var act = async () => await _eventBus.PublishAsync(testEvent);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task HandlesHandlerExceptionsGracefully()
        {
            // Arrange
            var successfulHandlerCalled = false;
            var failingHandlerCalled = false;
            var testEvent = new TestDomainEvent();

            _eventBus.Subscribe<TestDomainEvent>(e => {
                successfulHandlerCalled = true;
                return Task.CompletedTask;
            });

            _eventBus.Subscribe<TestDomainEvent>(e => {
                failingHandlerCalled = true;
                throw new InvalidOperationException("Handler failed");
            });

            // Act - should not throw even though one handler fails
            var act = async () => await _eventBus.PublishAsync(testEvent);
            await act.Should().NotThrowAsync();

            // Assert - both handlers were called
            successfulHandlerCalled.Should().BeTrue();
            failingHandlerCalled.Should().BeTrue();
        }

        [Fact]
        public async Task PublishesCorrectEventTypeToHandlers()
        {
            // Arrange
            var testEvent = new TestDomainEvent();
            var differentEvent = new AnotherTestDomainEvent();
            var testEventHandled = false;
            var differentEventHandled = false;

            _eventBus.Subscribe<TestDomainEvent>(e => {
                testEventHandled = true;
                return Task.CompletedTask;
            });

            _eventBus.Subscribe<AnotherTestDomainEvent>(e => {
                differentEventHandled = true;
                return Task.CompletedTask;
            });

            // Act - publish TestDomainEvent
            await _eventBus.PublishAsync(testEvent);

            // Assert
            testEventHandled.Should().BeTrue();
            differentEventHandled.Should().BeFalse();
        }
    }

    public class Subscribe : EventBusTests
    {
        [Fact]
        public void AddsHandlerForEventType()
        {
            // Arrange
            var handlerCalled = false;
            var testEvent = new TestDomainEvent();

            // Act
            _eventBus.Subscribe<TestDomainEvent>(e => {
                handlerCalled = true;
                return Task.CompletedTask;
            });

            // Assert
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>();
            subscribers.Should().HaveCount(1);
        }

        [Fact]
        public void AddsMultipleHandlersForSameEventType()
        {
            // Arrange
            var testEvent = new TestDomainEvent();

            // Act
            _eventBus.Subscribe<TestDomainEvent>(e => Task.CompletedTask);
            _eventBus.Subscribe<TestDomainEvent>(e => Task.CompletedTask);
            _eventBus.Subscribe<TestDomainEvent>(e => Task.CompletedTask);

            // Assert
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>();
            subscribers.Should().HaveCount(3);
        }
    }

    public class Unsubscribe : EventBusTests
    {
        [Fact]
        public void RemovesSpecificHandler()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var testEvent = new TestDomainEvent();

            var handler1 = new Func<TestDomainEvent, Task>(e => {
                handler1Called = true;
                return Task.CompletedTask;
            });

            var handler2 = new Func<TestDomainEvent, Task>(e => {
                handler2Called = true;
                return Task.CompletedTask;
            });

            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);

            // Act
            _eventBus.Unsubscribe(handler1);

            // Assert
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>().ToList();
            subscribers.Should().HaveCount(1);
            subscribers.Should().Contain(handler2);
            subscribers.Should().NotContain(handler1);
        }

        [Fact]
        public void RemovesAllHandlersWhenLastOneUnsubscribed()
        {
            // Arrange
            var handler = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);

            _eventBus.Subscribe(handler);

            // Act
            _eventBus.Unsubscribe(handler);

            // Assert
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>();
            subscribers.Should().BeEmpty();
        }

        [Fact]
        public void DoesNotThrowWhenUnsubscribingNonExistentHandler()
        {
            // Arrange
            var handler1 = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);
            var handler2 = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);

            _eventBus.Subscribe(handler1);

            // Act & Assert - should not throw
            var act = () => _eventBus.Unsubscribe(handler2);
            act.Should().NotThrow();
        }

        [Fact]
        public void DoesNotThrowWhenUnsubscribingFromEmptyEventType()
        {
            // Arrange
            var handler = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);

            // Act & Assert - should not throw
            var act = () => _eventBus.Unsubscribe(handler);
            act.Should().NotThrow();
        }
    }

    public class GetSubscribers : EventBusTests
    {
        [Fact]
        public void ReturnsEmptyEnumerableWhenNoHandlers()
        {
            // Act
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>();

            // Assert
            subscribers.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsAllSubscribersForEventType()
        {
            // Arrange
            var handler1 = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);
            var handler2 = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);
            var handler3 = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);

            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);
            _eventBus.Subscribe(handler3);

            // Act
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>().ToList();

            // Assert
            subscribers.Should().HaveCount(3);
            subscribers.Should().Contain(handler1);
            subscribers.Should().Contain(handler2);
            subscribers.Should().Contain(handler3);
        }

        [Fact]
        public void ReturnsSubscribersForDifferentEventTypesSeparately()
        {
            // Arrange
            var handler1 = new Func<TestDomainEvent, Task>(e => Task.CompletedTask);
            var handler2 = new Func<AnotherTestDomainEvent, Task>(e => Task.CompletedTask);

            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);

            // Act
            var testSubscribers = _eventBus.GetSubscribers<TestDomainEvent>().ToList();
            var anotherTestSubscribers = _eventBus.GetSubscribers<AnotherTestDomainEvent>().ToList();

            // Assert
            testSubscribers.Should().HaveCount(1);
            testSubscribers.Should().Contain(handler1);
            anotherTestSubscribers.Should().HaveCount(1);
            anotherTestSubscribers.Should().Contain(handler2);
        }
    }

    public class Clear : EventBusTests
    {
        [Fact]
        public void RemovesAllSubscriptions()
        {
            // Arrange
            _eventBus.Subscribe<TestDomainEvent>(e => Task.CompletedTask);
            _eventBus.Subscribe<AnotherTestDomainEvent>(e => Task.CompletedTask);
            _eventBus.Subscribe<TestDomainEvent>(e => Task.CompletedTask);

            // Act
            _eventBus.Clear();

            // Assert
            var testSubscribers = _eventBus.GetSubscribers<TestDomainEvent>();
            var anotherTestSubscribers = _eventBus.GetSubscribers<AnotherTestDomainEvent>();

            testSubscribers.Should().BeEmpty();
            anotherTestSubscribers.Should().BeEmpty();
        }
    }

    public class ThreadSafety : EventBusTests
    {
        [Fact]
        public async Task HandlesConcurrentSubscriptions()
        {
            // Arrange
            var tasks = new List<Task>();
            var testEvent = new TestDomainEvent();
            var handlerCallCount = 0;

            // Act - multiple threads subscribing
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    _eventBus.Subscribe<TestDomainEvent>(e => {
                        Interlocked.Increment(ref handlerCallCount);
                        return Task.CompletedTask;
                    });
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - all handlers should be registered
            var subscribers = _eventBus.GetSubscribers<TestDomainEvent>();
            subscribers.Should().HaveCount(10);

            // Publish to verify all work
            await _eventBus.PublishAsync(testEvent);

            // Handler should be called 10 times (once per subscriber)
            handlerCallCount.Should().Be(10);
        }

        [Fact]
        public async Task HandlesConcurrentPublishAndSubscribe()
        {
            // Arrange
            var publishCount = 0;
            var subscribeCount = 0;
            var testEvent = new TestDomainEvent();

            // Act - concurrent publish and subscribe
            var publishTask = Task.Run(async () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    await _eventBus.PublishAsync(testEvent);
                    Interlocked.Increment(ref publishCount);
                    await Task.Delay(10);
                }
            });

            var subscribeTask = Task.Run(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    _eventBus.Subscribe<TestDomainEvent>(e => Task.CompletedTask);
                    Interlocked.Increment(ref subscribeCount);
                    Thread.Sleep(10);
                }
            });

            await Task.WhenAll(publishTask, subscribeTask);

            // Assert
            publishCount.Should().Be(5);
            subscribeCount.Should().Be(5);
        }
    }

    // Test domain events for testing
    private class TestDomainEvent : DomainEvent { }
    private class AnotherTestDomainEvent : DomainEvent { }
}
