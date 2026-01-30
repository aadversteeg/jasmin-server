using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Infrastructure.McpServers.FileStorage;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.McpServers.FileStorage;

public class McpServerFileRepositoryTests : IDisposable
{
    private readonly string _testDirectory;

    public McpServerFileRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"mcp-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    private string CreateConfigFile(string content)
    {
        var filePath = Path.Combine(_testDirectory, "config.json");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    [Fact(DisplayName = "MFR-001: GetAll should return empty list when no servers configured")]
    public void MFR001()
    {
        var configPath = CreateConfigFile("""{"mcpServers": {}}""");
        var repository = new McpServerFileRepository(configPath);

        var result = repository.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "MFR-002: GetAll should return all configured servers")]
    public void MFR002()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "chronos": { "command": "docker" },
                "github": { "command": "npx" }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);

        var result = repository.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(s => s.Id.Value == "chronos");
        result.Value.Should().Contain(s => s.Id.Value == "github");
    }

    [Fact(DisplayName = "MFR-003: GetAll should return servers sorted by id")]
    public void MFR003()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "zebra": { "command": "npx" },
                "alpha": { "command": "docker" }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);

        var result = repository.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Id.Value.Should().Be("alpha");
        result.Value[1].Id.Value.Should().Be("zebra");
    }

    [Fact(DisplayName = "MFR-004: GetById should return None for non-existent server")]
    public void MFR004()
    {
        var configPath = CreateConfigFile("""{"mcpServers": {}}""");
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("non-existent").Value;

        var result = repository.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeFalse();
    }

    [Fact(DisplayName = "MFR-005: GetById should return server definition when found")]
    public void MFR005()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "chronos": {
                    "command": "docker",
                    "args": ["run", "--rm", "-i"],
                    "env": { "TZ": "Europe/Amsterdam" }
                }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("chronos").Value;

        var result = repository.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Id.Value.Should().Be("chronos");
        result.Value.Value.Command.Should().Be("docker");
        result.Value.Value.Args.Should().HaveCount(3);
        result.Value.Value.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "MFR-006: GetById should handle missing args and env")]
    public void MFR006()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "simple": { "command": "npx" }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("simple").Value;

        var result = repository.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Args.Should().BeEmpty();
        result.Value.Value.Env.Should().BeEmpty();
    }

    [Fact(DisplayName = "MFR-007: GetAll should return error when config file not found")]
    public void MFR007()
    {
        var repository = new McpServerFileRepository("/non/existent/path.json");

        var result = repository.GetAll();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.ConfigFileNotFound);
    }

    [Fact(DisplayName = "MFR-008: GetAll should return error when config file is invalid JSON")]
    public void MFR008()
    {
        var configPath = CreateConfigFile("not valid json");
        var repository = new McpServerFileRepository(configPath);

        var result = repository.GetAll();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.ConfigFileInvalid);
    }

    [Fact(DisplayName = "MFR-009: Create should add new server to config file")]
    public void MFR009()
    {
        var configPath = CreateConfigFile("""{"mcpServers": {}}""");
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("new-server").Value;
        var definition = new McpServerDefinition(
            id,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["ENV"] = "value" }.AsReadOnly());

        var result = repository.Create(definition);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Value.Should().Be("new-server");

        // Verify it was persisted
        var getResult = repository.GetById(id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.HasValue.Should().BeTrue();
        getResult.Value.Value.Command.Should().Be("docker");
    }

    [Fact(DisplayName = "MFR-010: Create should return error when server already exists")]
    public void MFR010()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "existing": { "command": "npx" }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("existing").Value;
        var definition = new McpServerDefinition(
            id, "docker", Array.Empty<string>().ToList().AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());

        var result = repository.Create(definition);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.DuplicateMcpServerName);
    }

    [Fact(DisplayName = "MFR-011: Update should modify existing server in config file")]
    public void MFR011()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "chronos": { "command": "docker", "args": ["old"] }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("chronos").Value;
        var definition = new McpServerDefinition(
            id,
            "npx",
            new List<string> { "new", "args" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());

        var result = repository.Update(definition);

        result.IsSuccess.Should().BeTrue();
        result.Value.Command.Should().Be("npx");

        // Verify it was persisted
        var getResult = repository.GetById(id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Value.Command.Should().Be("npx");
        getResult.Value.Value.Args.Should().HaveCount(2);
    }

    [Fact(DisplayName = "MFR-012: Update should return error when server does not exist")]
    public void MFR012()
    {
        var configPath = CreateConfigFile("""{"mcpServers": {}}""");
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("non-existent").Value;
        var definition = new McpServerDefinition(
            id, "docker", Array.Empty<string>().ToList().AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());

        var result = repository.Update(definition);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.McpServerNotFound);
    }

    [Fact(DisplayName = "MFR-013: Delete should remove server from config file")]
    public void MFR013()
    {
        var configPath = CreateConfigFile("""
        {
            "mcpServers": {
                "to-delete": { "command": "docker" },
                "to-keep": { "command": "npx" }
            }
        }
        """);
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("to-delete").Value;

        var result = repository.Delete(id);

        result.IsSuccess.Should().BeTrue();

        // Verify it was removed
        var getResult = repository.GetById(id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.HasValue.Should().BeFalse();

        // Verify other server still exists
        var keepId = McpServerName.Create("to-keep").Value;
        var keepResult = repository.GetById(keepId);
        keepResult.Value.HasValue.Should().BeTrue();
    }

    [Fact(DisplayName = "MFR-014: Delete should return error when server does not exist")]
    public void MFR014()
    {
        var configPath = CreateConfigFile("""{"mcpServers": {}}""");
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("non-existent").Value;

        var result = repository.Delete(id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.McpServerNotFound);
    }

    [Fact(DisplayName = "MFR-015: Create should persist with correct JSON structure")]
    public void MFR015()
    {
        var configPath = CreateConfigFile("""{"mcpServers": {}}""");
        var repository = new McpServerFileRepository(configPath);
        var id = McpServerName.Create("test-server").Value;
        var definition = new McpServerDefinition(
            id,
            "docker",
            new List<string> { "run", "-i" }.AsReadOnly(),
            new Dictionary<string, string> { ["KEY"] = "value" }.AsReadOnly());

        repository.Create(definition);

        var json = File.ReadAllText(configPath);
        json.Should().Contain("\"mcpServers\"");
        json.Should().Contain("\"test-server\"");
        json.Should().Contain("\"command\"");
        json.Should().Contain("\"docker\"");
    }
}
