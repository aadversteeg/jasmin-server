using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Infrastructure.WebApp.Controllers;
using Core.Infrastructure.WebApp.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
        _controller = new McpServersController(_mockService.Object);
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
        var response = okResult.Value as List<McpServerInfoResponse>;
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
        var response = okResult.Value as McpServerDefinitionResponse;
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
}
