using Ave.Extensions.Functional;
using Core.Application.Requests;
using Core.Domain.Models;
using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.Requests;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Infrastructure.WebApp.Controllers;

public class RequestsControllerTests
{
    private readonly Mock<IRequestStore> _mockStore;
    private readonly Mock<IRequestQueue> _mockQueue;
    private readonly Mock<IRequestCancellation> _mockCancellation;
    private readonly RequestsController _controller;

    public RequestsControllerTests()
    {
        _mockStore = new Mock<IRequestStore>();
        _mockQueue = new Mock<IRequestQueue>();
        _mockCancellation = new Mock<IRequestCancellation>();
        var statusOptions = Options.Create(new McpServerStatusOptions { DefaultTimeZone = "UTC" });
        _controller = new RequestsController(
            _mockStore.Object,
            _mockQueue.Object,
            _mockCancellation.Object,
            statusOptions);
    }

    [Fact(DisplayName = "RQC-001: GetStatus should return status of a pending request")]
    public void RQC001()
    {
        var request = CreateRequest();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));

        var result = _controller.GetStatus(request.Id.Value);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RequestStatusResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be("pending");
    }

    [Fact(DisplayName = "RQC-002: GetStatus should return 404 when request not found")]
    public void RQC002()
    {
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe<Request>.None);

        var result = _controller.GetStatus("nonexistent-id");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "RQC-003: GetStatus should return status of a running request")]
    public void RQC003()
    {
        var request = CreateRequest();
        request.MarkRunning();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));

        var result = _controller.GetStatus(request.Id.Value);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RequestStatusResponse;
        response!.Status.Should().Be("running");
    }

    [Fact(DisplayName = "RQC-004: GetStatus should return status of a cancelled request")]
    public void RQC004()
    {
        var request = CreateRequest();
        request.MarkCancelled();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));

        var result = _controller.GetStatus(request.Id.Value);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RequestStatusResponse;
        response!.Status.Should().Be("cancelled");
    }

    [Fact(DisplayName = "RQC-005: UpdateStatus should cancel a pending request")]
    public void RQC005()
    {
        var request = CreateRequest();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));
        _mockCancellation.Setup(x => x.Cancel(It.IsAny<RequestId>())).Returns(true);

        var result = _controller.UpdateStatus(request.Id.Value, new UpdateRequestStatusBody("cancelled"));

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RequestStatusResponse;
        response!.Status.Should().Be("cancelled");
        _mockCancellation.Verify(x => x.Cancel(It.IsAny<RequestId>()), Times.Once);
    }

    [Fact(DisplayName = "RQC-006: UpdateStatus should cancel a running request")]
    public void RQC006()
    {
        var request = CreateRequest();
        request.MarkRunning();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));
        _mockCancellation.Setup(x => x.Cancel(It.IsAny<RequestId>())).Returns(true);

        var result = _controller.UpdateStatus(request.Id.Value, new UpdateRequestStatusBody("cancelled"));

        result.Should().BeOfType<OkObjectResult>();
        _mockCancellation.Verify(x => x.Cancel(It.IsAny<RequestId>()), Times.Once);
    }

    [Fact(DisplayName = "RQC-007: UpdateStatus should return 404 when request not found")]
    public void RQC007()
    {
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe<Request>.None);

        var result = _controller.UpdateStatus("nonexistent-id", new UpdateRequestStatusBody("cancelled"));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "RQC-008: UpdateStatus should return 409 when request is completed")]
    public void RQC008()
    {
        var request = CreateRequest();
        request.MarkCompleted();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));

        var result = _controller.UpdateStatus(request.Id.Value, new UpdateRequestStatusBody("cancelled"));

        result.Should().BeOfType<ConflictObjectResult>();
        _mockCancellation.Verify(x => x.Cancel(It.IsAny<RequestId>()), Times.Never);
    }

    [Fact(DisplayName = "RQC-009: UpdateStatus should return 409 when request is failed")]
    public void RQC009()
    {
        var request = CreateRequest();
        request.MarkFailed([new Ave.Extensions.ErrorPaths.Error(new Ave.Extensions.ErrorPaths.ErrorCode("ERR"), "Error")]);
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));

        var result = _controller.UpdateStatus(request.Id.Value, new UpdateRequestStatusBody("cancelled"));

        result.Should().BeOfType<ConflictObjectResult>();
        _mockCancellation.Verify(x => x.Cancel(It.IsAny<RequestId>()), Times.Never);
    }

    [Fact(DisplayName = "RQC-010: UpdateStatus should return 200 when request is already cancelled")]
    public void RQC010()
    {
        var request = CreateRequest();
        request.MarkCancelled();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));

        var result = _controller.UpdateStatus(request.Id.Value, new UpdateRequestStatusBody("cancelled"));

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RequestStatusResponse;
        response!.Status.Should().Be("cancelled");
        _mockCancellation.Verify(x => x.Cancel(It.IsAny<RequestId>()), Times.Never);
    }

    [Fact(DisplayName = "RQC-011: UpdateStatus should return 400 for invalid status value")]
    public void RQC011()
    {
        var result = _controller.UpdateStatus("some-id", new UpdateRequestStatusBody("completed"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "RQC-012: UpdateStatus should return 400 for empty status value")]
    public void RQC012()
    {
        var result = _controller.UpdateStatus("some-id", new UpdateRequestStatusBody(""));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "RQC-013: UpdateStatus should accept case-insensitive cancelled value")]
    public void RQC013()
    {
        var request = CreateRequest();
        _mockStore.Setup(x => x.GetById(It.IsAny<RequestId>()))
            .Returns(Maybe.From(request));
        _mockCancellation.Setup(x => x.Cancel(It.IsAny<RequestId>())).Returns(true);

        var result = _controller.UpdateStatus(request.Id.Value, new UpdateRequestStatusBody("Cancelled"));

        result.Should().BeOfType<OkObjectResult>();
    }

    private static Request CreateRequest()
    {
        return new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/test-server");
    }
}
