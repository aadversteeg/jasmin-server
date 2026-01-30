using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models.McpServers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Infrastructure.WebApp.Controllers;

public class McpServersControllerTests
{
    private readonly Mock<IMcpServerService> _mockService;
    private readonly McpServersController _controller;

    public McpServersControllerTests()
    {
        _mockService = new Mock<IMcpServerService>();
        var statusOptions = Options.Create(new McpServerStatusOptions { DefaultTimeZone = "UTC" });
        _controller = new McpServersController(_mockService.Object, statusOptions);
    }

    [Fact(DisplayName = "MSC-001: GetAll should return OkResult with server list")]
    public void MSC001()
    {
        var chronosId = McpServerId.Create("chronos").Value;
        var githubId = McpServerId.Create("github").Value;
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
        _mockService.Setup(x => x.GetById(It.IsAny<McpServerId>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _controller.GetById("non-existent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact(DisplayName = "MSC-003: GetById should return OkResult with server definition when found")]
    public void MSC003()
    {
        var chronosId = McpServerId.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        _mockService.Setup(x => x.GetById(It.Is<McpServerId>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _controller.GetById("chronos");

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("chronos");
        response.Command.Should().Be("docker");
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
        var chronosId = McpServerId.Create("new-server").Value;
        var definition = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockService.Setup(x => x.Create(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));
        var request = new CreateRequest("new-server", "docker", ["run"], null);

        var result = _controller.Create(request);

        result.Should().BeOfType<CreatedAtRouteResult>();
        var createdResult = (CreatedAtRouteResult)result;
        createdResult.RouteName.Should().Be("GetMcpServerById");
        var response = createdResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Name.Should().Be("new-server");
    }

    [Fact(DisplayName = "MSC-006: Create should return Conflict when server already exists")]
    public void MSC006()
    {
        var error = Errors.DuplicateMcpServerId("existing");
        _mockService.Setup(x => x.Create(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Failure(error));
        var request = new CreateRequest("existing", "docker", null, null);

        var result = _controller.Create(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact(DisplayName = "MSC-007: Create should return BadRequest for empty name")]
    public void MSC007()
    {
        var request = new CreateRequest("", "docker", null, null);

        var result = _controller.Create(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-008: Update should return OkResult when successful")]
    public void MSC008()
    {
        var chronosId = McpServerId.Create("chronos").Value;
        var definition = new McpServerDefinition(
            chronosId,
            "npx",
            new List<string> { "-y", "package" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockService.Setup(x => x.Update(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));
        var request = new UpdateRequest("npx", ["-y", "package"], null);

        var result = _controller.Update("chronos", request);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as DetailsResponse;
        response.Should().NotBeNull();
        response!.Command.Should().Be("npx");
    }

    [Fact(DisplayName = "MSC-009: Update should return NotFound when server does not exist")]
    public void MSC009()
    {
        var error = Errors.McpServerNotFound("non-existent");
        _mockService.Setup(x => x.Update(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Failure(error));
        var request = new UpdateRequest("docker", null, null);

        var result = _controller.Update("non-existent", request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "MSC-010: Update should return BadRequest for empty id")]
    public void MSC010()
    {
        var request = new UpdateRequest("docker", null, null);

        var result = _controller.Update("", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "MSC-011: Delete should return NoContent when successful")]
    public void MSC011()
    {
        _mockService.Setup(x => x.Delete(It.Is<McpServerId>(id => id.Value == "chronos")))
            .Returns(Result<Unit, Error>.Success(Unit.Value));

        var result = _controller.Delete("chronos");

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact(DisplayName = "MSC-012: Delete should return NotFound when server does not exist")]
    public void MSC012()
    {
        var error = Errors.McpServerNotFound("non-existent");
        _mockService.Setup(x => x.Delete(It.IsAny<McpServerId>()))
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
}
