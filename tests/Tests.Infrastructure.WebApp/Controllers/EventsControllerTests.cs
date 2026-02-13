using Core.Application.Events;
using Core.Domain.Events;
using Core.Domain.Paging;
using Core.Infrastructure.Messaging.SSE;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.McpServers;
using Core.Infrastructure.WebApp.Models.Paging;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Infrastructure.WebApp.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventStore> _mockEventStore;
    private readonly SseClientManager _sseClientManager;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _mockEventStore = new Mock<IEventStore>();
        var loggerMock = new Mock<ILogger<SseClientManager>>();
        _sseClientManager = new SseClientManager(loggerMock.Object);
        var statusOptions = Options.Create(new McpServerStatusOptions { DefaultTimeZone = "UTC" });
        var jsonOptions = Options.Create(new JsonOptions());
        _controller = new EventsController(_mockEventStore.Object, _sseClientManager, statusOptions, jsonOptions);
    }

    [Fact(DisplayName = "EVC-001: GetEvents should return OkResult with paged events")]
    public void EVC001()
    {
        var events = new List<Event>
        {
            new(EventTypes.McpServer.Instance.Starting, "mcp-servers/chronos", DateTime.UtcNow),
            new(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<Event>(events, 1, 20, 2);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "EVC-002: GetEvents should filter by target")]
    public void EVC002()
    {
        var events = new List<Event>
        {
            new(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<Event>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.Is<string?>(t => t == "mcp-servers/chronos"),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(target: "mcp-servers/chronos");

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            "mcp-servers/chronos",
            It.IsAny<EventType?>(),
            It.IsAny<string?>(),
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-003: GetEvents should filter by event type")]
    public void EVC003()
    {
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.Is<EventType?>(et => et != null && et.Value.Value == "mcp-server.instance.started"),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(eventType: "mcp-server.instance.started");

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<string?>(),
            It.Is<EventType?>(et => et != null && et.Value.Value == "mcp-server.instance.started"),
            It.IsAny<string?>(),
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-004: GetEvents should filter by requestId")]
    public void EVC004()
    {
        var requestId = Guid.NewGuid().ToString();
        var events = new List<Event>
        {
            new(EventTypes.McpServer.ToolInvocation.Invoking, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow, null, requestId)
        };
        var pagedResult = new PagedResult<Event>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.Is<string?>(r => r == requestId),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(requestId: requestId);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<string?>(),
            It.IsAny<EventType?>(),
            requestId,
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-006: GetEvents should return BadRequest for invalid timezone")]
    public void EVC006()
    {
        var result = _controller.GetEvents(timeZone: "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var error = badRequest.Value as ErrorResponse;
        error.Should().NotBeNull();
        error!.Errors.Should().Contain(e => e.Code == "InvalidTimezone");
    }

    [Fact(DisplayName = "EVC-007: GetEvents should return BadRequest for invalid page number")]
    public void EVC007()
    {
        var result = _controller.GetEvents(page: 0);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "EVC-008: GetEvents should return BadRequest for invalid page size")]
    public void EVC008()
    {
        var result = _controller.GetEvents(pageSize: 0);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "EVC-009: GetEvents should return BadRequest for page size exceeding limit")]
    public void EVC009()
    {
        var result = _controller.GetEvents(pageSize: 101);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "EVC-010: GetEvents should treat empty target as no filter")]
    public void EVC010()
    {
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                null,
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(target: "");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "EVC-011: GetEvents should use ascending sort direction")]
    public void EVC011()
    {
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                SortDirection.Ascending))
            .Returns(pagedResult);

        var result = _controller.GetEvents(orderDirection: "asc");

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<string?>(),
            It.IsAny<EventType?>(),
            It.IsAny<string?>(),
            It.IsAny<DateRangeFilter?>(),
            SortDirection.Ascending), Times.Once);
    }

    [Fact(DisplayName = "EVC-012: GetEvents should use descending sort direction by default")]
    public void EVC012()
    {
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                SortDirection.Descending))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<string?>(),
            It.IsAny<EventType?>(),
            It.IsAny<string?>(),
            It.IsAny<DateRangeFilter?>(),
            SortDirection.Descending), Times.Once);
    }

    [Fact(DisplayName = "EVC-013: GetEvents should pass date range filter")]
    public void EVC013()
    {
        var fromDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.Is<DateRangeFilter?>(d => d != null && d.From == fromDate && d.To == toDate),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(from: fromDate, to: toDate);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<string?>(),
            It.IsAny<EventType?>(),
            It.IsAny<string?>(),
            It.Is<DateRangeFilter?>(d => d != null && d.From == fromDate && d.To == toDate),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-014: GetEvents should pass paging parameters")]
    public void EVC014()
    {
        var pagedResult = new PagedResult<Event>([], 3, 50, 100);
        _mockEventStore.Setup(x => x.GetEvents(
                It.Is<PagingParameters>(p => p.Page == 3 && p.PageSize == 50),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(page: 3, pageSize: 50);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Page.Should().Be(3);
        response.PageSize.Should().Be(50);
    }

    [Fact(DisplayName = "EVC-015: GetEvents should return empty list when no events match")]
    public void EVC015()
    {
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
        response.TotalItems.Should().Be(0);
    }

    [Fact(DisplayName = "EVC-016: GetEvents should include target in response")]
    public void EVC016()
    {
        var events = new List<Event>
        {
            new(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<Event>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items[0].Target.Should().Be("mcp-servers/chronos");
    }

    [Fact(DisplayName = "EVC-018: GetEvents should include requestId in response when present")]
    public void EVC018()
    {
        var requestId = Guid.NewGuid().ToString();
        var events = new List<Event>
        {
            new(EventTypes.McpServer.ToolInvocation.Invoking, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow, null, requestId)
        };
        var pagedResult = new PagedResult<Event>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<string?>(),
                It.IsAny<EventType?>(),
                It.IsAny<string?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items[0].RequestId.Should().Be(requestId);
    }

    [Fact(DisplayName = "EVC-020: GetEvents with null filters should not filter")]
    public void EVC020()
    {
        var pagedResult = new PagedResult<Event>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                null,
                null,
                null,
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(target: null, eventType: null, requestId: null);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            null,
            null,
            null,
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }
}
