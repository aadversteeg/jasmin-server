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
    private readonly Mock<IMcpServerConnectionStatusCache> _mockStatusCache;
    private readonly Mock<IEventStore> _mockEventStore;
    private readonly McpServerService _service;

    public McpServerServiceTests()
    {
        _mockRepository = new Mock<IMcpServerRepository>();
        _mockStatusCache = new Mock<IMcpServerConnectionStatusCache>();
        _mockEventStore = new Mock<IEventStore>();
        _service = new McpServerService(_mockRepository.Object, _mockStatusCache.Object, _mockEventStore.Object);
    }

    [Fact(DisplayName = "MSS-001: GetAll should delegate to repository")]
    public void MSS001()
    {
        var chronosId = McpServerName.Create("chronos").Value;
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
        var chronosId = McpServerName.Create("chronos").Value;
        var server = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        _mockRepository.Setup(x => x.GetById(It.Is<McpServerName>(id => id.Value == "chronos")))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(server)));

        var result = _service.GetById(chronosId);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Id.Value.Should().Be("chronos");
    }

    [Fact(DisplayName = "MSS-004: GetById should return None when repository returns None")]
    public void MSS004()
    {
        var id = McpServerName.Create("non-existent").Value;
        _mockRepository.Setup(x => x.GetById(It.IsAny<McpServerName>()))
            .Returns(Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None));

        var result = _service.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeFalse();
    }

    [Fact(DisplayName = "MSS-005: Create should delegate to repository")]
    public void MSS005()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var definition = new McpServerDefinition(
            chronosId,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockRepository.Setup(x => x.Create(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));

        var result = _service.Create(definition);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Value.Should().Be("chronos");
        _mockRepository.Verify(x => x.Create(definition), Times.Once);
    }

    [Fact(DisplayName = "MSS-006: Create should return error when repository fails")]
    public void MSS006()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var definition = new McpServerDefinition(
            chronosId, "docker", Array.Empty<string>().ToList().AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var error = Errors.DuplicateMcpServerName("chronos");
        _mockRepository.Setup(x => x.Create(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Failure(error));

        var result = _service.Create(definition);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.DuplicateMcpServerName);
    }

    [Fact(DisplayName = "MSS-007: Update should delegate to repository")]
    public void MSS007()
    {
        var chronosId = McpServerName.Create("chronos").Value;
        var definition = new McpServerDefinition(
            chronosId,
            "npx",
            new List<string> { "-y", "package" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        _mockRepository.Setup(x => x.Update(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Success(definition));

        var result = _service.Update(definition);

        result.IsSuccess.Should().BeTrue();
        result.Value.Command.Should().Be("npx");
        _mockRepository.Verify(x => x.Update(definition), Times.Once);
    }

    [Fact(DisplayName = "MSS-008: Update should return error when server not found")]
    public void MSS008()
    {
        var id = McpServerName.Create("non-existent").Value;
        var definition = new McpServerDefinition(
            id, "docker", Array.Empty<string>().ToList().AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var error = Errors.McpServerNotFound("non-existent");
        _mockRepository.Setup(x => x.Update(It.IsAny<McpServerDefinition>()))
            .Returns(Result<McpServerDefinition, Error>.Failure(error));

        var result = _service.Update(definition);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.McpServerNotFound);
    }

    [Fact(DisplayName = "MSS-009: Delete should delegate to repository")]
    public void MSS009()
    {
        var id = McpServerName.Create("chronos").Value;
        _mockRepository.Setup(x => x.Delete(It.Is<McpServerName>(i => i.Value == "chronos")))
            .Returns(Result<Unit, Error>.Success(Unit.Value));

        var result = _service.Delete(id);

        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(x => x.Delete(id), Times.Once);
    }

    [Fact(DisplayName = "MSS-010: Delete should return error when server not found")]
    public void MSS010()
    {
        var id = McpServerName.Create("non-existent").Value;
        var error = Errors.McpServerNotFound("non-existent");
        _mockRepository.Setup(x => x.Delete(It.IsAny<McpServerName>()))
            .Returns(Result<Unit, Error>.Failure(error));

        var result = _service.Delete(id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.McpServerNotFound);
    }
}
