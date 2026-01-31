using Core.Application.Events;
using Core.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Infrastructure.Messaging;

public class HandlerRunnerTests
{
    private readonly Mock<ILogger> _loggerMock;

    public HandlerRunnerTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact(DisplayName = "HR-001: TryEnqueue should return true when queue has space")]
    public void HR001()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        var settings = new EventPublisherSettings { ChannelCapacity = 10 };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        var result = runner.TryEnqueue(new TestEvent("test"));

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "HR-002: TryEnqueue with DropNewest policy should silently drop new items when full")]
    public async Task HR002()
    {
        // With DropNewest (BoundedChannelFullMode.DropWrite), TryWrite returns true
        // but the new item is silently dropped. We verify only the first items are processed.
        var processedEvents = new List<string>();
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => processedEvents.Add(e.Message))
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings
        {
            ChannelCapacity = 2,
            OverflowPolicy = OverflowPolicy.DropNewest
        };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        // Enqueue 3 items, but capacity is 2 with DropNewest - the 3rd should be dropped
        runner.TryEnqueue(new TestEvent("1"));
        runner.TryEnqueue(new TestEvent("2"));
        runner.TryEnqueue(new TestEvent("3")); // This will be silently dropped

        using var cts = new CancellationTokenSource();
        await runner.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner.StopAsync(CancellationToken.None);

        // Only first 2 items should be processed
        processedEvents.Should().HaveCount(2);
        processedEvents.Should().Contain("1");
        processedEvents.Should().Contain("2");
        processedEvents.Should().NotContain("3");
    }

    [Fact(DisplayName = "HR-003: ExecuteAsync should process events in order")]
    public async Task HR003()
    {
        var processedEvents = new List<string>();
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => processedEvents.Add(e.Message))
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings { ChannelCapacity = 10 };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        runner.TryEnqueue(new TestEvent("first"));
        runner.TryEnqueue(new TestEvent("second"));
        runner.TryEnqueue(new TestEvent("third"));

        using var cts = new CancellationTokenSource();
        await runner.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner.StopAsync(CancellationToken.None);

        processedEvents.Should().ContainInOrder("first", "second", "third");
    }

    [Fact(DisplayName = "HR-004: ExecuteAsync should continue processing after handler exception")]
    public async Task HR004()
    {
        var processedEvents = new List<string>();
        var callCount = 0;
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("Handler failed");
                }
                processedEvents.Add(e.Message);
            })
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings { ChannelCapacity = 10 };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        runner.TryEnqueue(new TestEvent("first"));
        runner.TryEnqueue(new TestEvent("second"));
        runner.TryEnqueue(new TestEvent("third"));

        using var cts = new CancellationTokenSource();
        await runner.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner.StopAsync(CancellationToken.None);

        processedEvents.Should().Contain("first");
        processedEvents.Should().Contain("third");
        processedEvents.Should().NotContain("second");
    }

    [Fact(DisplayName = "HR-005: EnqueueBlocking should enqueue event")]
    public void HR005()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        var settings = new EventPublisherSettings { ChannelCapacity = 10 };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        var action = () => runner.EnqueueBlocking(new TestEvent("test"));

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "HR-006: DropOldest policy should allow new events when queue is full")]
    public void HR006()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        var settings = new EventPublisherSettings
        {
            ChannelCapacity = 2,
            OverflowPolicy = OverflowPolicy.DropOldest
        };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        runner.TryEnqueue(new TestEvent("1"));
        runner.TryEnqueue(new TestEvent("2"));
        var result = runner.TryEnqueue(new TestEvent("3"));

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "HR-007: Handler should be called with correct event")]
    public async Task HR007()
    {
        TestEvent? capturedEvent = null;
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var settings = new EventPublisherSettings { ChannelCapacity = 10 };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        var testEvent = new TestEvent("specific-message");
        runner.TryEnqueue(testEvent);

        using var cts = new CancellationTokenSource();
        await runner.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner.StopAsync(CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Message.Should().Be("specific-message");
    }

    [Fact(DisplayName = "HR-008: Handler exception should be logged")]
    public async Task HR008()
    {
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var settings = new EventPublisherSettings { ChannelCapacity = 10 };
        var runner = new HandlerRunner<TestEvent>(handlerMock.Object, settings, _loggerMock.Object);

        runner.TryEnqueue(new TestEvent("test"));

        using var cts = new CancellationTokenSource();
        await runner.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await runner.StopAsync(CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

public record TestEvent(string Message);
