using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory;

public class McpServerRequestProcessorServiceTests
{
    private readonly Mock<IMcpServerRequestQueue> _queueMock;
    private readonly Mock<IMcpServerRequestStore> _storeMock;
    private readonly Mock<IMcpServerInstanceManager> _instanceManagerMock;
    private readonly Mock<IMcpServerConnectionStatusCache> _statusCacheMock;
    private readonly Mock<ILogger<McpServerRequestProcessorService>> _loggerMock;

    public McpServerRequestProcessorServiceTests()
    {
        _queueMock = new Mock<IMcpServerRequestQueue>();
        _storeMock = new Mock<IMcpServerRequestStore>();
        _instanceManagerMock = new Mock<IMcpServerInstanceManager>();
        _statusCacheMock = new Mock<IMcpServerConnectionStatusCache>();
        _loggerMock = new Mock<ILogger<McpServerRequestProcessorService>>();
    }

    [Fact(DisplayName = "MRPS-001: ProcessInvokeTool should call InvokeToolAsync with correct parameters")]
    public async Task MRPS001()
    {
        // Arrange
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var instanceId = McpServerInstanceId.Create();
        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            instanceId,
            "get_current_date_and_time",
            input);

        var toolResult = new McpToolInvocationResult(
            new List<McpToolContentBlock>
            {
                new("text", "Current time: 2024-01-15T10:30:00+01:00", null, null, null)
            },
            null,
            false);

        _instanceManagerMock
            .Setup(m => m.InvokeToolAsync(
                serverName,
                instanceId,
                "get_current_date_and_time",
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                requestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<McpToolInvocationResult, Error>.Success(toolResult));

        // Act - directly test request processing via reflection or by calling the method
        // Since ProcessRequestAsync is private, we test the behavior through the queue
        var cts = new CancellationTokenSource();
        _queueMock
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var service = new McpServerRequestProcessorService(
            _queueMock.Object,
            _storeMock.Object,
            _instanceManagerMock.Object,
            _statusCacheMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100); // Give time for processing
        await service.StopAsync(CancellationToken.None);

        // Assert
        _instanceManagerMock.Verify(m => m.InvokeToolAsync(
            serverName,
            instanceId,
            "get_current_date_and_time",
            It.IsAny<IReadOnlyDictionary<string, object?>?>(),
            requestId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "MRPS-002: ProcessInvokeTool should mark request completed with output on success")]
    public async Task MRPS002()
    {
        // Arrange
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var instanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            instanceId,
            "test_tool");

        var toolResult = new McpToolInvocationResult(
            new List<McpToolContentBlock>
            {
                new("text", "Result", null, null, null)
            },
            null,
            false);

        _instanceManagerMock
            .Setup(m => m.InvokeToolAsync(
                It.IsAny<McpServerName>(),
                It.IsAny<McpServerInstanceId>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<McpToolInvocationResult, Error>.Success(toolResult));

        var cts = new CancellationTokenSource();
        _queueMock
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var service = new McpServerRequestProcessorService(
            _queueMock.Object,
            _storeMock.Object,
            _instanceManagerMock.Object,
            _statusCacheMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert
        request.Status.Should().Be(McpServerRequestStatus.Completed);
        request.Output.Should().NotBeNull();
    }

    [Fact(DisplayName = "MRPS-003: ProcessInvokeTool should mark request failed on error")]
    public async Task MRPS003()
    {
        // Arrange
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var instanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            instanceId,
            "test_tool");

        var error = new Error(ErrorCodes.ToolInvocationFailed, "Tool not found");

        _instanceManagerMock
            .Setup(m => m.InvokeToolAsync(
                It.IsAny<McpServerName>(),
                It.IsAny<McpServerInstanceId>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<McpToolInvocationResult, Error>.Failure(error));

        var cts = new CancellationTokenSource();
        _queueMock
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var service = new McpServerRequestProcessorService(
            _queueMock.Object,
            _storeMock.Object,
            _instanceManagerMock.Object,
            _statusCacheMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert
        request.Status.Should().Be(McpServerRequestStatus.Failed);
        request.Errors.Should().NotBeNull();
        request.Errors.Should().HaveCount(1);
        request.Errors![0].Code.Should().Be("TOOL_INVOCATION_FAILED");
    }

    [Fact(DisplayName = "MRPS-004: ProcessInvokeTool should fail without instance ID")]
    public async Task MRPS004()
    {
        // Arrange
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        // Note: Creating request without targetInstanceId by using reflection or a custom constructor
        // In reality, the mapper would prevent this, but we test the processor's validation
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            null, // No instance ID
            "test_tool");

        var cts = new CancellationTokenSource();
        _queueMock
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var service = new McpServerRequestProcessorService(
            _queueMock.Object,
            _storeMock.Object,
            _instanceManagerMock.Object,
            _statusCacheMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert
        request.Status.Should().Be(McpServerRequestStatus.Failed);
        request.Errors.Should().NotBeNull();
        request.Errors![0].Code.Should().Be("INSTANCE_ID_REQUIRED_FOR_INVOKE_TOOL");
    }

    [Fact(DisplayName = "MRPS-005: ProcessInvokeTool should fail without tool name")]
    public async Task MRPS005()
    {
        // Arrange
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var instanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            instanceId,
            null); // No tool name

        var cts = new CancellationTokenSource();
        _queueMock
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var service = new McpServerRequestProcessorService(
            _queueMock.Object,
            _storeMock.Object,
            _instanceManagerMock.Object,
            _statusCacheMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert
        request.Status.Should().Be(McpServerRequestStatus.Failed);
        request.Errors.Should().NotBeNull();
        request.Errors![0].Code.Should().Be("TOOL_NAME_REQUIRED");
    }

    [Fact(DisplayName = "MRPS-006: ProcessInvokeTool should update store after completion")]
    public async Task MRPS006()
    {
        // Arrange
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var instanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            instanceId,
            "test_tool");

        var toolResult = new McpToolInvocationResult(
            new List<McpToolContentBlock> { new("text", "Result", null, null, null) },
            null,
            false);

        _instanceManagerMock
            .Setup(m => m.InvokeToolAsync(
                It.IsAny<McpServerName>(),
                It.IsAny<McpServerInstanceId>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<McpToolInvocationResult, Error>.Success(toolResult));

        var cts = new CancellationTokenSource();
        _queueMock
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        var service = new McpServerRequestProcessorService(
            _queueMock.Object,
            _storeMock.Object,
            _instanceManagerMock.Object,
            _statusCacheMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        // Assert - store should be updated twice: once for Running, once for Completed
        _storeMock.Verify(s => s.Update(request), Times.AtLeast(2));
    }
}
