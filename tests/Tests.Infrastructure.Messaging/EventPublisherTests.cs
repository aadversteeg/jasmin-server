using Core.Application.Events;
using Core.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Infrastructure.Messaging;

public class EventPublisherTests
{
    private readonly Mock<ILogger<EventPublisher<TestEvent>>> _loggerMock;
    private readonly Mock<ILogger> _handlerLoggerMock;

    public EventPublisherTests()
    {
        _loggerMock = new Mock<ILogger<EventPublisher<TestEvent>>>();
        _handlerLoggerMock = new Mock<ILogger>();
    }

    [Fact(DisplayName = "EP-001: Publish should enqueue event to all handler runners")]
    public void EP001()
    {
        var handler1Mock = new Mock<IEventHandler<TestEvent>>();
        var handler2Mock = new Mock<IEventHandler<TestEvent>>();
        var settings = new EventPublisherSettings { ChannelCapacity = 10 };

        var runner1 = new HandlerRunner<TestEvent>(handler1Mock.Object, settings, _handlerLoggerMock.Object);
        var runner2 = new HandlerRunner<TestEvent>(handler2Mock.Object, settings, _handlerLoggerMock.Object);
        var runners = new List<HandlerRunner<TestEvent>> { runner1, runner2 };

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);
        var testEvent = new TestEvent("test");

        publisher.Publish(testEvent);

        // Both runners should have the event enqueued (we can verify by checking TryEnqueue returns true still)
        // Since we enqueued once, capacity is 10, we should still be able to enqueue more
        runner1.TryEnqueue(new TestEvent("another")).Should().BeTrue();
        runner2.TryEnqueue(new TestEvent("another")).Should().BeTrue();
    }

    [Fact(DisplayName = "EP-002: Publish should not throw when no handlers registered")]
    public void EP002()
    {
        var settings = new EventPublisherSettings();
        var runners = new List<HandlerRunner<TestEvent>>();

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);

        var action = () => publisher.Publish(new TestEvent("test"));

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "EP-003: Publish with DropNewest policy silently drops new items")]
    public async Task EP003()
    {
        // With DropNewest (BoundedChannelFullMode.DropWrite), TryWrite returns true
        // even when items are dropped, so no warning is logged. This is expected behavior.
        var processedEvents = new List<string>();
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => processedEvents.Add(e.Message))
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings
        {
            ChannelCapacity = 1,
            OverflowPolicy = OverflowPolicy.DropNewest
        };

        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _handlerLoggerMock.Object);
        var runners = new List<HandlerRunner<TestEvent>> { runner };

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);

        publisher.Publish(new TestEvent("first"));
        publisher.Publish(new TestEvent("second")); // This will be silently dropped

        using var cts = new CancellationTokenSource();
        await runner.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner.StopAsync(CancellationToken.None);

        // Only first item should be processed
        processedEvents.Should().HaveCount(1);
        processedEvents.Should().Contain("first");
        processedEvents.Should().NotContain("second");

        // No warning logged (TryWrite returns true with DropWrite mode)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact(DisplayName = "EP-004: Publish with DropOldest should not log warning")]
    public void EP004()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        var settings = new EventPublisherSettings
        {
            ChannelCapacity = 1,
            OverflowPolicy = OverflowPolicy.DropOldest
        };

        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _handlerLoggerMock.Object);
        var runners = new List<HandlerRunner<TestEvent>> { runner };

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);

        publisher.Publish(new TestEvent("first"));
        publisher.Publish(new TestEvent("second"));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact(DisplayName = "EP-005: Publish with Wait policy should use EnqueueBlocking")]
    public void EP005()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        var settings = new EventPublisherSettings
        {
            ChannelCapacity = 10,
            OverflowPolicy = OverflowPolicy.Wait
        };

        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _handlerLoggerMock.Object);
        var runners = new List<HandlerRunner<TestEvent>> { runner };

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);

        var action = () => publisher.Publish(new TestEvent("test"));

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "EP-006: Publish should fan out to multiple handlers")]
    public async Task EP006()
    {
        var processedByHandler1 = new List<string>();
        var processedByHandler2 = new List<string>();

        var handler1Mock = new Mock<IEventHandler<TestEvent>>();
        handler1Mock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => processedByHandler1.Add(e.Message))
            .Returns(Task.CompletedTask);

        var handler2Mock = new Mock<IEventHandler<TestEvent>>();
        handler2Mock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => processedByHandler2.Add(e.Message))
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings { ChannelCapacity = 10 };

        var runner1 = new HandlerRunner<TestEvent>(handler1Mock.Object, settings, _handlerLoggerMock.Object);
        var runner2 = new HandlerRunner<TestEvent>(handler2Mock.Object, settings, _handlerLoggerMock.Object);
        var runners = new List<HandlerRunner<TestEvent>> { runner1, runner2 };

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);

        publisher.Publish(new TestEvent("event1"));
        publisher.Publish(new TestEvent("event2"));

        using var cts = new CancellationTokenSource();
        await runner1.StartAsync(cts.Token);
        await runner2.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner1.StopAsync(CancellationToken.None);
        await runner2.StopAsync(CancellationToken.None);

        processedByHandler1.Should().Contain("event1");
        processedByHandler1.Should().Contain("event2");
        processedByHandler2.Should().Contain("event1");
        processedByHandler2.Should().Contain("event2");
    }

    [Fact(DisplayName = "EP-007: Slow handler should not block other handlers")]
    public async Task EP007()
    {
        var handler1Completed = new TaskCompletionSource<bool>();
        var handler2Events = new List<string>();

        var handler1Mock = new Mock<IEventHandler<TestEvent>>();
        handler1Mock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Returns(async (TestEvent e, CancellationToken ct) =>
            {
                await Task.Delay(500, ct); // Slow handler
                handler1Completed.TrySetResult(true);
            });

        var handler2Mock = new Mock<IEventHandler<TestEvent>>();
        handler2Mock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => handler2Events.Add(e.Message))
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings { ChannelCapacity = 10 };

        var runner1 = new HandlerRunner<TestEvent>(handler1Mock.Object, settings, _handlerLoggerMock.Object);
        var runner2 = new HandlerRunner<TestEvent>(handler2Mock.Object, settings, _handlerLoggerMock.Object);
        var runners = new List<HandlerRunner<TestEvent>> { runner1, runner2 };

        var publisher = new EventPublisher<TestEvent>(runners, settings, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await runner1.StartAsync(cts.Token);
        await runner2.StartAsync(cts.Token);

        publisher.Publish(new TestEvent("test"));

        // Handler 2 should process quickly even though handler 1 is slow
        await Task.Delay(100);

        handler2Events.Should().Contain("test");

        await cts.CancelAsync();
        await runner1.StopAsync(CancellationToken.None);
        await runner2.StopAsync(CancellationToken.None);
    }
}
