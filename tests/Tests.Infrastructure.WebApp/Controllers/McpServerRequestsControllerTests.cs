using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.McpServers.Requests;
using Core.Infrastructure.WebApp.Models.Paging;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Infrastructure.WebApp.Controllers;

public class McpServerRequestsControllerTests
{
    private readonly Mock<IMcpServerService> _mockService;
    private readonly Mock<IMcpServerRequestStore> _mockRequestStore;
    private readonly Mock<IMcpServerRequestQueue> _mockRequestQueue;
    private readonly Mock<IEventStore> _mockEventStore;
    private readonly McpServerRequestsController _controller;

    public McpServerRequestsControllerTests()
    {
        _mockService = new Mock<IMcpServerService>();
        _mockRequestStore = new Mock<IMcpServerRequestStore>();
        _mockRequestQueue = new Mock<IMcpServerRequestQueue>();
        _mockEventStore = new Mock<IEventStore>();
        var statusOptions = Options.Create(new McpServerStatusOptions { DefaultTimeZone = "UTC" });
        _controller = new McpServerRequestsController(
            _mockService.Object,
            _mockRequestStore.Object,
            _mockRequestQueue.Object,
            _mockEventStore.Object,
            statusOptions);
    }

    [Fact(DisplayName = "MSRC-001: Create should return AcceptedAtRoute for start request")]
    public void MSRC001()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var request = new CreateRequestRequest("start", null, null, null);
        var result = _controller.Create("chronos", request);

        result.Should().BeOfType<AcceptedAtRouteResult>();
        var acceptedResult = (AcceptedAtRouteResult)result;
        acceptedResult.RouteName.Should().Be("GetMcpServerRequestById");
        _mockRequestStore.Verify(x => x.Add(It.IsAny<McpServerRequest>()), Times.Once);
        _mockRequestQueue.Verify(x => x.Enqueue(It.IsAny<McpServerRequest>()), Times.Once);
    }

    [Fact(DisplayName = "MSRC-002: Create should return NotFound for non-existent server")]
    public void MSRC002()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var request = new CreateRequestRequest("start", null, null, null);
        var result = _controller.Create("non-existent", request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSRC-003: Create should return BadRequest for invalid server name")]
    public void MSRC003()
    {
        var request = new CreateRequestRequest("start", null, null, null);
        var result = _controller.Create("", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSRC-004: Create InvokeTool should record ToolInvocationAccepted event")]
    public void MSRC004()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        var instanceId = McpServerInstanceId.Create();
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var request = new CreateRequestRequest("invokeTool", instanceId.Value, "get_time", input);
        var result = _controller.Create("chronos", request);

        result.Should().BeOfType<AcceptedAtRouteResult>();
        _mockEventStore.Verify(x => x.RecordEvent(
            It.Is<McpServerName>(n => n.Value == "chronos"),
            McpServerEventType.ToolInvocationAccepted,
            null,
            It.Is<McpServerInstanceId?>(i => i != null && i.Value == instanceId.Value),
            It.IsAny<McpServerRequestId?>(),
            null,
            null,
            It.Is<McpServerToolInvocationEventData?>(d => d != null && d.ToolName == "get_time")),
            Times.Once);
    }

    [Fact(DisplayName = "MSRC-005: Create start request should NOT record event")]
    public void MSRC005()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var request = new CreateRequestRequest("start", null, null, null);
        _controller.Create("chronos", request);

        _mockEventStore.Verify(x => x.RecordEvent(
            It.IsAny<McpServerName>(),
            It.IsAny<McpServerEventType>(),
            It.IsAny<IReadOnlyList<McpServerEventError>?>(),
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<McpServerEventConfiguration?>(),
            It.IsAny<McpServerEventConfiguration?>(),
            It.IsAny<McpServerToolInvocationEventData?>()), Times.Never);
    }

    [Fact(DisplayName = "MSRC-006: Create stop request should NOT record event")]
    public void MSRC006()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        var instanceId = McpServerInstanceId.Create();
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var request = new CreateRequestRequest("stop", instanceId.Value, null, null);
        _controller.Create("chronos", request);

        _mockEventStore.Verify(x => x.RecordEvent(
            It.IsAny<McpServerName>(),
            It.IsAny<McpServerEventType>(),
            It.IsAny<IReadOnlyList<McpServerEventError>?>(),
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<McpServerEventConfiguration?>(),
            It.IsAny<McpServerEventConfiguration?>(),
            It.IsAny<McpServerToolInvocationEventData?>()), Times.Never);
    }

    [Fact(DisplayName = "MSRC-007: GetById should return NotFound for non-existent request")]
    public void MSRC007()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _mockRequestStore.Setup(x => x.GetById(It.IsAny<McpServerRequestId>()))
            .Returns(Maybe<McpServerRequest>.None);

        var result = _controller.GetById("chronos", Guid.NewGuid().ToString());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSRC-008: GetById should return OkResult for existing request")]
    public void MSRC008()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        _mockRequestStore.Setup(x => x.GetById(It.Is<McpServerRequestId>(id => id.Value == requestId.Value)))
            .Returns(Maybe.From(request));

        var result = _controller.GetById("chronos", requestId.Value);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as RequestResponse;
        response.Should().NotBeNull();
        response!.RequestId.Should().Be(requestId.Value);
    }

    [Fact(DisplayName = "MSRC-009: GetById should return NotFound when request belongs to different server")]
    public void MSRC009()
    {
        var chronosName = McpServerName.Create("chronos").Value;
        var githubName = McpServerName.Create("github").Value;
        var requestId = McpServerRequestId.Create();
        var request = new McpServerRequest(requestId, githubName, McpServerRequestAction.Start);
        _mockRequestStore.Setup(x => x.GetById(It.Is<McpServerRequestId>(id => id.Value == requestId.Value)))
            .Returns(Maybe.From(request));

        var result = _controller.GetById("chronos", requestId.Value);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSRC-010: GetAll should return OkResult with paged requests")]
    public void MSRC010()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        var requests = new List<McpServerRequest>
        {
            new(McpServerRequestId.Create(), serverName, McpServerRequestAction.Start),
            new(McpServerRequestId.Create(), serverName, McpServerRequestAction.Stop)
        };
        var pagedResult = new PagedResult<McpServerRequest>(requests, 1, 20, 2);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockRequestStore.Setup(x => x.GetByServerName(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.IsAny<PagingParameters>(),
                It.IsAny<DateRangeFilter>(),
                It.IsAny<string>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetAll("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<RequestResponse>;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "MSRC-011: GetAll should return NotFound for non-existent server")]
    public void MSRC011()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetAll("non-existent");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSRC-012: GetAll should return BadRequest for invalid timezone")]
    public void MSRC012()
    {
        var result = _controller.GetAll("chronos", "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSRC-013: Create InvokeTool with input should include input in event data")]
    public void MSRC013()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        var instanceId = McpServerInstanceId.Create();
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var input = JsonSerializer.SerializeToElement(new { param1 = "value1", param2 = 123 });
        var request = new CreateRequestRequest("invokeTool", instanceId.Value, "test_tool", input);
        _controller.Create("chronos", request);

        _mockEventStore.Verify(x => x.RecordEvent(
            It.IsAny<McpServerName>(),
            McpServerEventType.ToolInvocationAccepted,
            null,
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            null,
            null,
            It.Is<McpServerToolInvocationEventData?>(d =>
                d != null &&
                d.ToolName == "test_tool" &&
                d.Input.HasValue)),
            Times.Once);
    }

    [Fact(DisplayName = "MSRC-014: Create should return BadRequest for invalid action")]
    public void MSRC014()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var request = new CreateRequestRequest("invalid_action", null, null, null);
        var result = _controller.Create("chronos", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSRC-015: Create InvokeTool without instanceId should return BadRequest")]
    public void MSRC015()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var request = new CreateRequestRequest("invokeTool", null, "test_tool", null);
        var result = _controller.Create("chronos", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSRC-016: Create InvokeTool without toolName should return BadRequest")]
    public void MSRC016()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(serverName);
        var instanceId = McpServerInstanceId.Create();
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var request = new CreateRequestRequest("invokeTool", instanceId.Value, null, null);
        var result = _controller.Create("chronos", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
