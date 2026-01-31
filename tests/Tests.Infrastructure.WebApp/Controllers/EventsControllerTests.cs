using Core.Application.McpServers;
using Core.Domain.McpServers;
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
        var serverName = McpServerName.Create("chronos").Value;
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.Starting, DateTime.UtcNow),
            new(serverName, McpServerEventType.Started, DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 2);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
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

    [Fact(DisplayName = "EVC-002: GetEvents should filter by serverName")]
    public void EVC002()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.Started, DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.Is<McpServerName?>(n => n != null && n.Value == "chronos"),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(serverName: "chronos");

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.Is<McpServerName?>(n => n != null && n.Value == "chronos"),
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-003: GetEvents should filter by instanceId")]
    public void EVC003()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = McpServerInstanceId.Create();
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.Started, DateTime.UtcNow, null, instanceId)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.Is<McpServerInstanceId?>(i => i != null && i.Value == instanceId.Value),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(instanceId: instanceId.Value);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<McpServerName?>(),
            It.Is<McpServerInstanceId?>(i => i != null && i.Value == instanceId.Value),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-004: GetEvents should filter by requestId")]
    public void EVC004()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.ToolInvoking, DateTime.UtcNow, null, null, requestId)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.Is<McpServerRequestId?>(r => r != null && r.Value == requestId.Value),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(requestId: requestId.Value);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<McpServerName?>(),
            It.IsAny<McpServerInstanceId?>(),
            It.Is<McpServerRequestId?>(r => r != null && r.Value == requestId.Value),
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-005: GetEvents should combine multiple filters")]
    public void EVC005()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = McpServerInstanceId.Create();
        var requestId = McpServerRequestId.Create();
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.ToolInvoked, DateTime.UtcNow, null, instanceId, requestId)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.Is<McpServerName?>(n => n != null && n.Value == "chronos"),
                It.Is<McpServerInstanceId?>(i => i != null && i.Value == instanceId.Value),
                It.Is<McpServerRequestId?>(r => r != null && r.Value == requestId.Value),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(
            serverName: "chronos",
            instanceId: instanceId.Value,
            requestId: requestId.Value);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.Is<McpServerName?>(n => n != null && n.Value == "chronos"),
            It.Is<McpServerInstanceId?>(i => i != null && i.Value == instanceId.Value),
            It.Is<McpServerRequestId?>(r => r != null && r.Value == requestId.Value),
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
        error!.Errors.Should().Contain(e => e.Code == "INVALID_TIMEZONE");
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

    [Fact(DisplayName = "EVC-010: GetEvents should treat empty server name as no filter")]
    public void EVC010()
    {
        var pagedResult = new PagedResult<McpServerEvent>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                null, // empty serverName should pass null as filter
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(serverName: "");

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            null,
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<DateRangeFilter?>(),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-011: GetEvents should use ascending sort direction")]
    public void EVC011()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var pagedResult = new PagedResult<McpServerEvent>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                SortDirection.Ascending))
            .Returns(pagedResult);

        var result = _controller.GetEvents(orderDirection: "asc");

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<McpServerName?>(),
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<DateRangeFilter?>(),
            SortDirection.Ascending), Times.Once);
    }

    [Fact(DisplayName = "EVC-012: GetEvents should use descending sort direction by default")]
    public void EVC012()
    {
        var pagedResult = new PagedResult<McpServerEvent>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                SortDirection.Descending))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<McpServerName?>(),
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.IsAny<DateRangeFilter?>(),
            SortDirection.Descending), Times.Once);
    }

    [Fact(DisplayName = "EVC-013: GetEvents should pass date range filter")]
    public void EVC013()
    {
        var fromDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var pagedResult = new PagedResult<McpServerEvent>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.Is<DateRangeFilter?>(d => d != null && d.From == fromDate && d.To == toDate),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(from: fromDate, to: toDate);

        result.Should().BeOfType<OkObjectResult>();
        _mockEventStore.Verify(x => x.GetEvents(
            It.IsAny<PagingParameters>(),
            It.IsAny<McpServerName?>(),
            It.IsAny<McpServerInstanceId?>(),
            It.IsAny<McpServerRequestId?>(),
            It.Is<DateRangeFilter?>(d => d != null && d.From == fromDate && d.To == toDate),
            It.IsAny<SortDirection>()), Times.Once);
    }

    [Fact(DisplayName = "EVC-014: GetEvents should pass paging parameters")]
    public void EVC014()
    {
        var pagedResult = new PagedResult<McpServerEvent>([], 3, 50, 100);
        _mockEventStore.Setup(x => x.GetEvents(
                It.Is<PagingParameters>(p => p.Page == 3 && p.PageSize == 50),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
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
        var pagedResult = new PagedResult<McpServerEvent>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
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

    [Fact(DisplayName = "EVC-016: GetEvents should include serverName in response")]
    public void EVC016()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.Started, DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items[0].ServerName.Should().Be("chronos");
    }

    [Fact(DisplayName = "EVC-017: GetEvents should include instanceId in response when present")]
    public void EVC017()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = McpServerInstanceId.Create();
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.Started, DateTime.UtcNow, null, instanceId)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items[0].InstanceId.Should().Be(instanceId.Value);
    }

    [Fact(DisplayName = "EVC-018: GetEvents should include requestId in response when present")]
    public void EVC018()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var requestId = McpServerRequestId.Create();
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.ToolInvoking, DateTime.UtcNow, null, null, requestId)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items[0].RequestId.Should().Be(requestId.Value);
    }

    [Fact(DisplayName = "EVC-019: GetEvents should include errors in response when present")]
    public void EVC019()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var errors = new List<McpServerEventError>
        {
            new("ConnectionError", "Connection refused")
        }.AsReadOnly();
        var events = new List<McpServerEvent>
        {
            new(serverName, McpServerEventType.StartFailed, DateTime.UtcNow, errors)
        };
        var pagedResult = new PagedResult<McpServerEvent>(events, 1, 20, 1);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                It.IsAny<McpServerName?>(),
                It.IsAny<McpServerInstanceId?>(),
                It.IsAny<McpServerRequestId?>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items[0].Errors.Should().NotBeNull();
        response.Items[0].Errors.Should().HaveCount(1);
        response.Items[0].Errors![0].Code.Should().Be("ConnectionError");
    }

    [Fact(DisplayName = "EVC-020: GetEvents with null filters should not filter")]
    public void EVC020()
    {
        var pagedResult = new PagedResult<McpServerEvent>([], 1, 20, 0);
        _mockEventStore.Setup(x => x.GetEvents(
                It.IsAny<PagingParameters>(),
                null,
                null,
                null,
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(pagedResult);

        var result = _controller.GetEvents(serverName: null, instanceId: null, requestId: null);

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
