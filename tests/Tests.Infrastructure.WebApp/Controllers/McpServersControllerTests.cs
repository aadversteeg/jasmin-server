using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Instances;
using Core.Infrastructure.WebApp.Models.McpServers.Prompts;
using Core.Infrastructure.WebApp.Models.McpServers.Resources;
using Core.Infrastructure.WebApp.Models.McpServers.Tools;
using Core.Infrastructure.WebApp.Models.Paging;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Infrastructure.WebApp.Controllers;

public class McpServersControllerTests
{
    private readonly Mock<IMcpServerService> _mockService;
    private readonly Mock<IMcpServerConnectionStatusCache> _mockStatusCache;
    private readonly Mock<IMcpServerRequestStore> _mockRequestStore;
    private readonly Mock<IMcpServerInstanceManager> _mockInstanceManager;
    private readonly McpServersController _controller;

    public McpServersControllerTests()
    {
        _mockService = new Mock<IMcpServerService>();
        _mockStatusCache = new Mock<IMcpServerConnectionStatusCache>();
        _mockRequestStore = new Mock<IMcpServerRequestStore>();
        _mockInstanceManager = new Mock<IMcpServerInstanceManager>();
        var statusOptions = Options.Create(new McpServerStatusOptions { DefaultTimeZone = "UTC" });
        _controller = new McpServersController(
            _mockService.Object,
            _mockStatusCache.Object,
            _mockRequestStore.Object,
            _mockInstanceManager.Object,
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

    [Fact(DisplayName = "MSC-015: GetById without include should not return configuration")]
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
        response!.Configuration.Should().BeNull();
    }

    [Fact(DisplayName = "MSC-016: GetById with invalid include should return BadRequest")]
    public void MSC016()
    {
        var result = _controller.GetById("chronos", "invalid");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-020: GetById with invalid timezone should return BadRequest")]
    public void MSC020()
    {
        var result = _controller.GetById("chronos", null, "Invalid/Timezone");

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
        var requests = new List<McpServerRequest>
        {
            new(McpServerRequestId.Create(), chronosId, McpServerRequestAction.Start)
        };
        var instances = new List<McpServerInstanceInfo>
        {
            new(McpServerInstanceId.Create(), chronosId, DateTime.UtcNow, null, null)
        };
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockRequestStore.Setup(x => x.GetByServerName(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(requests);
        _mockInstanceManager.Setup(x => x.GetRunningInstances(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(instances);

        var result = _controller.GetById("chronos", "all");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Configuration.Should().NotBeNull();
        response.Requests.Should().NotBeNull();
        response.Requests.Should().HaveCount(1);
        response.Instances.Should().NotBeNull();
        response.Instances.Should().HaveCount(1);
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

    [Fact(DisplayName = "MSC-028: GetInstances should return list of instances for existing server")]
    public void MSC028()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var config = new McpServerEventConfiguration(
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var instances = new List<McpServerInstanceInfo>
        {
            new(McpServerInstanceId.Create(), chronosId, new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), config, null),
            new(McpServerInstanceId.Create(), chronosId, new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc), config, null)
        };
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetRunningInstances(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(instances);

        var result = _controller.GetInstances("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as InstanceListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items[0].ServerName.Should().Be("chronos");
        response.Items[0].Configuration.Should().NotBeNull();
        response.Items[0].Configuration!.Command.Should().Be("docker");
    }

    [Fact(DisplayName = "MSC-029: GetInstances should return NotFound for non-existent server")]
    public void MSC029()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetInstances("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-030: GetInstances should return BadRequest for invalid timezone")]
    public void MSC030()
    {
        var result = _controller.GetInstances("chronos", "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-031: GetInstances should return empty list when no instances running")]
    public void MSC031()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetRunningInstances(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(new List<McpServerInstanceInfo>());

        var result = _controller.GetInstances("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as InstanceListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "MSC-032: GetInstance should return instance details for existing instance")]
    public void MSC032()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var config = new McpServerEventConfiguration(
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        var instance = new McpServerInstanceInfo(
            instanceId,
            chronosId,
            new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            config,
            null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstance("chronos", instanceId.Value);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as InstanceResponse;
        response.Should().NotBeNull();
        response!.InstanceId.Should().Be(instanceId.Value);
        response.ServerName.Should().Be("chronos");
        response.Configuration.Should().NotBeNull();
        response.Configuration!.Command.Should().Be("docker");
        response.Configuration.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "MSC-033: GetInstance should return NotFound for non-existent server")]
    public void MSC033()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetInstance("non-existent", Guid.NewGuid().ToString());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-034: GetInstance should return NotFound for non-existent instance")]
    public void MSC034()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.IsAny<McpServerName>(),
                It.IsAny<McpServerInstanceId>()))
            .Returns((McpServerInstanceInfo?)null);

        var result = _controller.GetInstance("chronos", Guid.NewGuid().ToString());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-035: GetInstance should return BadRequest for invalid timezone")]
    public void MSC035()
    {
        var result = _controller.GetInstance("chronos", Guid.NewGuid().ToString(), "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-036: GetById with include=instances should return server with instances")]
    public void MSC036()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var config = new McpServerEventConfiguration(
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var instances = new List<McpServerInstanceInfo>
        {
            new(McpServerInstanceId.Create(), chronosId, DateTime.UtcNow, config, null)
        };
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetRunningInstances(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(instances);

        var result = _controller.GetById("chronos", "instances");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("chronos");
        response.Instances.Should().NotBeNull();
        response.Instances.Should().HaveCount(1);
        response.Instances![0].Configuration.Should().NotBeNull();
        response.Configuration.Should().BeNull(); // Not included
    }

    [Fact(DisplayName = "MSC-037: GetById without include should not return instances")]
    public void MSC037()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _controller.GetById("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Instances.Should().BeNull();
        _mockInstanceManager.Verify(x => x.GetRunningInstances(It.IsAny<McpServerName>()), Times.Never);
    }

    [Fact(DisplayName = "MSC-038: GetTools should return list of tools for existing server")]
    public void MSC038()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var tools = new List<McpTool>
        {
            new("read_file", "Read File", "Reads a file", null),
            new("write_file", "Write File", "Writes a file", null)
        };
        var metadata = new McpServerMetadata(tools, null, null, DateTime.UtcNow, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns(metadata);

        var result = _controller.GetTools("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ToolListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items[0].Name.Should().Be("read_file");
        response.Items[1].Name.Should().Be("write_file");
    }

    [Fact(DisplayName = "MSC-039: GetTools should return NotFound for non-existent server")]
    public void MSC039()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetTools("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-040: GetTools should return empty list when no metadata")]
    public void MSC040()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns((McpServerMetadata?)null);

        var result = _controller.GetTools("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ToolListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "MSC-041: GetPrompts should return list of prompts for existing server")]
    public void MSC041()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var prompts = new List<McpPrompt>
        {
            new("generate", "Generate Text", "Generates text", null),
            new("summarize", "Summarize", "Summarizes content", null)
        };
        var metadata = new McpServerMetadata(null, prompts, null, DateTime.UtcNow, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns(metadata);

        var result = _controller.GetPrompts("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PromptListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items[0].Name.Should().Be("generate");
        response.Items[1].Name.Should().Be("summarize");
    }

    [Fact(DisplayName = "MSC-042: GetPrompts should return NotFound for non-existent server")]
    public void MSC042()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetPrompts("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-043: GetResources should return list of resources for existing server")]
    public void MSC043()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var resources = new List<McpResource>
        {
            new("config", "file:///config.json", "Config", null, "application/json"),
            new("data", "file:///data.txt", "Data", null, "text/plain")
        };
        var metadata = new McpServerMetadata(null, null, resources, DateTime.UtcNow, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns(metadata);

        var result = _controller.GetResources("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ResourceListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items[0].Name.Should().Be("config");
        response.Items[1].Name.Should().Be("data");
    }

    [Fact(DisplayName = "MSC-044: GetResources should return NotFound for non-existent server")]
    public void MSC044()
    {
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetResources("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-045: GetById with include=tools should return server with tools")]
    public void MSC045()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var tools = new List<McpTool> { new("read_file", "Read File", null, null) };
        var metadata = new McpServerMetadata(tools, null, null, DateTime.UtcNow, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns(metadata);

        var result = _controller.GetById("chronos", "tools");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Tools.Should().NotBeNull();
        response.Tools.Should().HaveCount(1);
        response.Tools![0].Name.Should().Be("read_file");
        response.Prompts.Should().BeNull();
        response.Resources.Should().BeNull();
    }

    [Fact(DisplayName = "MSC-046: GetById with include=prompts should return server with prompts")]
    public void MSC046()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var prompts = new List<McpPrompt> { new("generate", "Generate", null, null) };
        var metadata = new McpServerMetadata(null, prompts, null, DateTime.UtcNow, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns(metadata);

        var result = _controller.GetById("chronos", "prompts");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Prompts.Should().NotBeNull();
        response.Prompts.Should().HaveCount(1);
        response.Prompts![0].Name.Should().Be("generate");
        response.Tools.Should().BeNull();
        response.Resources.Should().BeNull();
    }

    [Fact(DisplayName = "MSC-047: GetById with include=resources should return server with resources")]
    public void MSC047()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var resources = new List<McpResource> { new("config", "file:///config", null, null, null) };
        var metadata = new McpServerMetadata(null, null, resources, DateTime.UtcNow, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockStatusCache.Setup(x => x.GetMetadata(It.IsAny<McpServerId>()))
            .Returns(metadata);

        var result = _controller.GetById("chronos", "resources");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Resources.Should().NotBeNull();
        response.Resources.Should().HaveCount(1);
        response.Resources![0].Name.Should().Be("config");
        response.Tools.Should().BeNull();
        response.Prompts.Should().BeNull();
    }

    [Fact(DisplayName = "MSC-048: GetTools should return BadRequest for invalid timezone")]
    public void MSC048()
    {
        var result = _controller.GetTools("chronos", "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-049: GetPrompts should return BadRequest for invalid timezone")]
    public void MSC049()
    {
        var result = _controller.GetPrompts("chronos", "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-050: GetResources should return BadRequest for invalid timezone")]
    public void MSC050()
    {
        var result = _controller.GetResources("chronos", "Invalid/Timezone");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-051: GetInstance with include=tools should return instance with tools")]
    public void MSC051()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var tools = new List<McpTool> { new("read_file", "Read File", null, null) };
        var metadata = new McpServerMetadata(tools, null, null, DateTime.UtcNow, null);
        var instance = new McpServerInstanceInfo(instanceId, chronosId, DateTime.UtcNow, null, metadata);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstance("chronos", instanceId.Value, "tools");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as InstanceResponse;
        response.Should().NotBeNull();
        response!.Tools.Should().NotBeNull();
        response.Tools.Should().HaveCount(1);
        response.Tools![0].Name.Should().Be("read_file");
        response.Prompts.Should().BeNull();
        response.Resources.Should().BeNull();
    }

    [Fact(DisplayName = "MSC-052: GetInstance with include=all should return instance with all metadata")]
    public void MSC052()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var tools = new List<McpTool> { new("tool1", null, null, null) };
        var prompts = new List<McpPrompt> { new("prompt1", null, null, null) };
        var resources = new List<McpResource> { new("resource1", "file:///r1", null, null, null) };
        var metadata = new McpServerMetadata(tools, prompts, resources, DateTime.UtcNow, null);
        var instance = new McpServerInstanceInfo(instanceId, chronosId, DateTime.UtcNow, null, metadata);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstance("chronos", instanceId.Value, "all");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as InstanceResponse;
        response.Should().NotBeNull();
        response!.Tools.Should().HaveCount(1);
        response.Prompts.Should().HaveCount(1);
        response.Resources.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MSC-053: GetInstance with invalid include should return BadRequest")]
    public void MSC053()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _controller.GetInstance("chronos", Guid.NewGuid().ToString(), "invalid");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-054: GetInstanceTools should return tools for existing instance")]
    public void MSC054()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var tools = new List<McpTool>
        {
            new("read_file", "Read File", "Reads a file", "{\"type\":\"object\"}"),
            new("write_file", "Write File", "Writes a file", null)
        };
        var metadata = new McpServerMetadata(tools, null, null, new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), null);
        var instance = new McpServerInstanceInfo(instanceId, chronosId, DateTime.UtcNow, null, metadata);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstanceTools("chronos", instanceId.Value);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ToolListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items[0].Name.Should().Be("read_file");
        response.RetrievedAt.Should().StartWith("2024-01-15T10:30:00");
    }

    [Fact(DisplayName = "MSC-055: GetInstanceTools should return NotFound for non-existent instance")]
    public void MSC055()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.IsAny<McpServerName>(),
                It.IsAny<McpServerInstanceId>()))
            .Returns((McpServerInstanceInfo?)null);

        var result = _controller.GetInstanceTools("chronos", Guid.NewGuid().ToString());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-056: GetInstancePrompts should return prompts for existing instance")]
    public void MSC056()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var prompts = new List<McpPrompt>
        {
            new("generate", "Generate Text", "Generates text", null)
        };
        var metadata = new McpServerMetadata(null, prompts, null, DateTime.UtcNow, null);
        var instance = new McpServerInstanceInfo(instanceId, chronosId, DateTime.UtcNow, null, metadata);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstancePrompts("chronos", instanceId.Value);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as PromptListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(1);
        response.Items[0].Name.Should().Be("generate");
    }

    [Fact(DisplayName = "MSC-057: GetInstanceResources should return resources for existing instance")]
    public void MSC057()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var resources = new List<McpResource>
        {
            new("config", "file:///config.json", "Config", "App config", "application/json")
        };
        var metadata = new McpServerMetadata(null, null, resources, DateTime.UtcNow, null);
        var instance = new McpServerInstanceInfo(instanceId, chronosId, DateTime.UtcNow, null, metadata);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstanceResources("chronos", instanceId.Value);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ResourceListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(1);
        response.Items[0].Name.Should().Be("config");
        response.Items[0].MimeType.Should().Be("application/json");
    }

    [Fact(DisplayName = "MSC-058: GetInstanceTools should return empty list for instance with no metadata")]
    public void MSC058()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(chronosId);
        var instanceId = McpServerInstanceId.Create();
        var instance = new McpServerInstanceInfo(instanceId, chronosId, DateTime.UtcNow, null, null);
        _mockService.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));
        _mockInstanceManager.Setup(x => x.GetInstance(
                It.Is<McpServerName>(id => id.Value == "chronos"),
                It.Is<McpServerInstanceId>(id => id.Value == instanceId.Value)))
            .Returns(instance);

        var result = _controller.GetInstanceTools("chronos", instanceId.Value);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ToolListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
    }
}
