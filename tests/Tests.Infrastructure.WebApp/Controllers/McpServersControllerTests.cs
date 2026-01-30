using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models.McpServers;
using Core.Infrastructure.WebApp.Models.Paging;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

using McpServerEvent = Core.Domain.McpServers.McpServerEvent;

namespace Tests.Infrastructure.WebApp.Controllers;

public class McpServersControllerTests
{
    private readonly Mock<IMcpServerService> _mockService;
    private readonly Mock<IMcpServerConnectionStatusCache> _mockStatusCache;
    private readonly Mock<IMcpServerRequestStore> _mockRequestStore;
    private readonly McpServersController _controller;

    public McpServersControllerTests()
    {
        _mockService = new Mock<IMcpServerService>();
        _mockStatusCache = new Mock<IMcpServerConnectionStatusCache>();
        _mockRequestStore = new Mock<IMcpServerRequestStore>();
        var statusOptions = Options.Create(new McpServerStatusOptions { DefaultTimeZone = "UTC" });
        _controller = new McpServersController(
            _mockService.Object,
            _mockStatusCache.Object,
            _mockRequestStore.Object,
            statusOptions);

        // Default status cache behavior
        _mockStatusCache.Setup(x => x.GetOrCreateId(It.IsAny<McpServerName>()))
            .Returns((McpServerName name) => McpServerId.Create());
        _mockStatusCache.Setup(x => x.GetEntry(It.IsAny<McpServerId>()))
            .Returns(new McpServerStatusCacheEntry(McpServerConnectionStatus.Unknown, null));
    }

    [Fact(DisplayName = "MSC-001: GetAll should return OkResult with server list")]
    public void MSC001()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var githubId = McpServerName.Create("github").Value;
        var servers = new List<McpServerInfo>
        {
            new(chronosId, "docker"),
            new(githubId, "docker")
        };
        _mockService.Setup(x => x.GetAll())
            .Returns(Result<IReadOnlyList<McpServerInfo>, Error>.Success(servers));

        var result = _controller.GetAll();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<ListResponse>;
        response.Should().HaveCount(2);
        response.Should().Contain(r => r.Name == "chronos");
    }

    [Fact(DisplayName = "MSC-002: GetById should return NotFound when server does not exist")]
    public void MSC002()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetById("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-003: GetById should return OkResult with server details when found")]
    public void MSC003()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetEntry(It.IsAny<McpServerId>()))
            .Returns(new McpServerStatusCacheEntry(McpServerConnectionStatus.Verified, DateTime.UtcNow));

        var result = _controller.GetById("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("chronos");
        response.Status.Should().Be("verified");
        response.Configuration.Should().BeNull(); // Not included by default
    }

    [Fact(DisplayName = "MSC-004: GetById should return BadRequest for empty id")]
    public void MSC004()
    {
        var result = _controller.GetById("");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-005: Create should return CreatedAtRoute when successful")]
    public void MSC005()
    {
        var chronosId = McpServerName.Create("new-server").Value;
        var definition = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockService.Setup(x => x.Create(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));
        var request = new CreateRequest("new-server", new ConfigurationRequest("docker", ["run"], null));

        var result = _controller.Create(request);

        result.Should().BeOfType<CreatedAtRouteResult>();
        var createdResult = (CreatedAtRouteResult)result;
        createdResult.RouteName.Should().Be("GetMcpServerById");
        var response = createdResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("new-server");
        response.Configuration.Should().NotBeNull();
        response.Configuration!.Command.Should().Be("docker");
    }

    [Fact(DisplayName = "MSC-006: Create should return Conflict when server already exists")]
    public void MSC006()
    {
        var error = Errors.DuplicateMcpServerName("existing");
        _mockService.Setup(x => x.Create(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Failure(error));
        var request = new CreateRequest("existing", new ConfigurationRequest("docker", null, null));

        var result = _controller.Create(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact(DisplayName = "MSC-007: Create should return BadRequest for empty name")]
    public void MSC007()
    {
        var request = new CreateRequest("", new ConfigurationRequest("docker", null, null));

        var result = _controller.Create(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-008: UpdateConfiguration should return OkResult when successful")]
    public void MSC008()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var definition = new McpServerDefinition(
            chronosId,
            "npx",
            new List<string> { "-y", "package" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockService.Setup(x => x.Update(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));
        var request = new ConfigurationRequest("npx", ["-y", "package"], null);

        var result = _controller.UpdateConfiguration("chronos", request);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ConfigurationResponse;
        response.Should().NotBeNull();
        response!.Command.Should().Be("npx");
    }

    [Fact(DisplayName = "MSC-009: UpdateConfiguration should return NotFound when server does not exist")]
    public void MSC009()
    {
        var error = Errors.McpServerNotFound("non-existent");
        _mockService.Setup(x => x.Update(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Failure(error));
        var request = new ConfigurationRequest("docker", null, null);

        var result = _controller.UpdateConfiguration("non-existent", request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSC-010: UpdateConfiguration should return BadRequest for empty id")]
    public void MSC010()
    {
        var request = new ConfigurationRequest("docker", null, null);

        var result = _controller.UpdateConfiguration("", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-011: Delete should return NoContent when successful")]
    public void MSC011()
    {
        _mockService.Setup(x => x.Delete(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Unit, Error>.Success(Unit.Value));

        var result = _controller.Delete("chronos");

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact(DisplayName = "MSC-012: Delete should return NotFound when server does not exist")]
    public void MSC012()
    {
        var error = Errors.McpServerNotFound("non-existent");
        _mockService.Setup(x => x.Delete(It.IsAny<McpServerName>()))
            .Returns(Result<Unit, Error>.Failure(error));

        var result = _controller.Delete("non-existent");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSC-013: Delete should return BadRequest for empty id")]
    public void MSC013()
    {
        var result = _controller.Delete("");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-014: GetById with include=events should return server with events")]
    public void MSC014()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var events = new List<McpServerEvent>
        {
            new(McpServerEventType.Starting, new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)),
            new(McpServerEventType.Started, new DateTime(2024, 1, 15, 10, 0, 1, DateTimeKind.Utc))
        };
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockService.Setup(x => x.GetEvents(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<IReadOnlyList<McpServerEvent>, Error>.Success(events));

        var result = _controller.GetById("chronos", "events");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("chronos");
        response.Events.Should().NotBeNull();
        response.Events.Should().HaveCount(2);
        response.Events![0].EventType.Should().Be("Starting");
        response.Events[1].EventType.Should().Be("Started");
    }

    [Fact(DisplayName = "MSC-015: GetById without include should not return events or configuration")]
    public void MSC015()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _controller.GetById("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Events.Should().BeNull();
        response.Configuration.Should().BeNull();
    }

    [Fact(DisplayName = "MSC-016: GetById with invalid include should return BadRequest")]
    public void MSC016()
    {
        var result = _controller.GetById("chronos", "invalid");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-017: GetEvents should return paged events for existing server")]
    public void MSC017()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string>().AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var events = new List<McpServerEvent>
        {
            new(McpServerEventType.Starting, new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)),
            new(McpServerEventType.Started, new DateTime(2024, 1, 15, 10, 0, 1, DateTimeKind.Utc))
        };
        var pagedEvents = new PagedResult<McpServerEvent>(events, 1, 20, 2);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockService.Setup(x => x.GetEvents(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.IsAny<PagingParameters>(),
                It.IsAny<DateRangeFilter?>(),
                It.IsAny<SortDirection>()))
            .Returns(Result<PagedResult<McpServerEvent>, Error>.Success(pagedEvents));

        var result = _controller.GetEvents("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PagedResponse<EventResponse>;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items[0].EventType.Should().Be("Starting");
        response.Page.Should().Be(1);
        response.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "MSC-018: GetEvents should return NotFound for non-existent server")]
    public void MSC018()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetEvents("non-existent");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSC-019: GetEvents should return BadRequest for invalid timezone")]
    public void MSC019()
    {
        var result = _controller.GetEvents("chronos", timeZone: "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-020: GetById with invalid timezone should return BadRequest")]
    public void MSC020()
    {
        var result = _controller.GetById("chronos", null, "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-021: GetEvents should return BadRequest for invalid page")]
    public void MSC021()
    {
        var result = _controller.GetEvents("chronos", page: 0);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-022: GetEvents should return BadRequest for invalid pageSize")]
    public void MSC022()
    {
        var result = _controller.GetEvents("chronos", pageSize: 101);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-023: GetById with include=configuration should return server with configuration")]
    public void MSC023()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _controller.GetById("chronos", "configuration");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Configuration.Should().NotBeNull();
        response.Configuration!.Command.Should().Be("docker");
        response.Configuration.Args.Should().BeEquivalentTo(new[] { "run", "--rm" });
        response.Configuration.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "MSC-024: GetById with include=all should return all sub-resources")]
    public void MSC024()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var events = new List<McpServerEvent>
        {
            new(McpServerEventType.Starting, new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc))
        };
        var requests = new List<McpServerRequest>
        {
            new(McpServerRequestId.Create(), chronosId, McpServerRequestAction.Start)
        };
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockService.Setup(x => x.GetEvents(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<IReadOnlyList<McpServerEvent>, Error>.Success(events));
        _mockRequestStore.Setup(x => x.GetByServerName(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(requests);

        var result = _controller.GetById("chronos", "all");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Configuration.Should().NotBeNull();
        response.Events.Should().NotBeNull();
        response.Events.Should().HaveCount(1);
        response.Requests.Should().NotBeNull();
        response.Requests.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MSC-025: GetConfiguration should return configuration for existing server")]
    public void MSC025()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _controller.GetConfiguration("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ConfigurationResponse;
        response.Should().NotBeNull();
        response!.Command.Should().Be("docker");
        response.Args.Should().BeEquivalentTo(new[] { "run" });
        response.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "MSC-026: GetConfiguration should return NotFound for non-existent server")]
    public void MSC026()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetConfiguration("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-027: Create without configuration should succeed")]
    public void MSC027()
    {
        var serverName = McpServerName.Create("new-server").Value;
        var definition = new McpServerDefinition(serverName);
        _mockService.Setup(x => x.Create(It.Is<McpServerDefinition>(d => d.Id.Value == "new-server" && !d.HasConfiguration)))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));

        var request = new CreateRequest("new-server", null);

        var result = _controller.Create(request);

        result.Should().BeOfType<CreatedAtRouteResult>();
        var createdResult = (CreatedAtRouteResult)result;
        var response = createdResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("new-server");
        response.Configuration.Should().BeNull();
    }
}
