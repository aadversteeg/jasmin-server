using Ave.Extensions.Functional;
using Core.Application.Requests;
using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.InMemory.Requests;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory.Requests;

public class RequestProcessorServiceTests
{
    private readonly Mock<IRequestStore> _mockStore;
    private readonly Mock<IRequestQueue> _mockQueue;
    private readonly Mock<IRequestHandlerRegistry> _mockRegistry;
    private readonly RequestProcessorService _service;

    public RequestProcessorServiceTests()
    {
        _mockStore = new Mock<IRequestStore>();
        _mockQueue = new Mock<IRequestQueue>();
        _mockRegistry = new Mock<IRequestHandlerRegistry>();
        var logger = new Mock<ILogger<RequestProcessorService>>();
        _service = new RequestProcessorService(
            _mockQueue.Object,
            _mockStore.Object,
            _mockRegistry.Object,
            logger.Object);
    }

    [Fact(DisplayName = "RPS-001: Cancel should return false when request not found")]
    public void RPS001()
    {
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe<Request>.None);

        var result = _service.Cancel(RequestId.From("nonexistent"));

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "RPS-002: Cancel should return false when request is completed")]
    public void RPS002()
    {
        var request = CreateRequest();
        request.MarkCompleted();
        _mockStore.Setup(x => x.GetById(request.Id))
            .Returns(Maybe.From(request));

        var result = _service.Cancel(request.Id);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "RPS-003: Cancel should return false when request is failed")]
    public void RPS003()
    {
        var request = CreateRequest();
        request.MarkFailed([new Ave.Extensions.ErrorPaths.Error(new Ave.Extensions.ErrorPaths.ErrorCode("ERR"), "Error")]);
        _mockStore.Setup(x => x.GetById(request.Id))
            .Returns(Maybe.From(request));

        var result = _service.Cancel(request.Id);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "RPS-004: Cancel should return false when request is already cancelled")]
    public void RPS004()
    {
        var request = CreateRequest();
        request.MarkCancelled();
        _mockStore.Setup(x => x.GetById(request.Id))
            .Returns(Maybe.From(request));

        var result = _service.Cancel(request.Id);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "RPS-005: Cancel should mark pending request as cancelled and update store")]
    public void RPS005()
    {
        var request = CreateRequest();
        _mockStore.Setup(x => x.GetById(request.Id))
            .Returns(Maybe.From(request));

        var result = _service.Cancel(request.Id);

        result.Should().BeTrue();
        request.Status.Should().Be(RequestStatus.Cancelled);
        _mockStore.Verify(x => x.Update(request), Times.Once);
    }

    private static Request CreateRequest()
    {
        return new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/test-server");
    }
}
