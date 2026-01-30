using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.Application.McpServers;

public class McpServerServiceTests
{
    private readonly Mock<IMcpServerRepository> _mockRepository;
    private readonly McpServerService _service;

    public McpServerServiceTests()
    {
        _mockRepository = new Mock<IMcpServerRepository>();
        _service = new McpServerService(_mockRepository.Object);
    }

    [Fact(DisplayName = "MSS-001: GetAll should delegate to repository")]
    public void MSS001()
    {
        var chronosId = McpServerId.Create("chronos").Value;
        var servers = new List<McpServerInfo> { new(chronosId, "docker") };
        _mockRepository.Setup(x => x.GetAll())
            .Returns(Result<IReadOnlyList<McpServerInfo>, Error>.Success(servers));

        var result = _service.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Value.Should().Be("chronos");
    }

    [Fact(DisplayName = "MSS-002: GetAll should return repository error on failure")]
    public void MSS002()
    {
        var error = Errors.ConfigFileNotFound("/path/to/config.json");
        _mockRepository.Setup(x => x.GetAll())
            .Returns(Result<IReadOnlyList<McpServerInfo>, Error>.Failure(error));

        var result = _service.GetAll();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.ConfigFileNotFound);
    }

    [Fact(DisplayName = "MSS-003: GetById should delegate to repository")]
    public void MSS003()
    {
        var chronosId = McpServerId.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        _mockRepository.Setup(x => x.GetById(It.Is<McpServerId>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _service.GetById(chronosId);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Id.Value.Should().Be("chronos");
    }

    [Fact(DisplayName = "MSS-004: GetById should return None when repository returns None")]
    public void MSS004()
    {
        var id = McpServerId.Create("non-existent").Value;
        _mockRepository.Setup(x => x.GetById(It.IsAny<McpServerId>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _service.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeFalse();
    }
}
