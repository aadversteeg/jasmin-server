using Core.Domain.McpServers;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory;

public class McpServerConnectionStatusCacheTests
{
    private readonly McpServerConnectionStatusCache _cache;

    public McpServerConnectionStatusCacheTests()
    {
        _cache = new McpServerConnectionStatusCache();
    }

    [Fact(DisplayName = "MCSC-001: GetOrCreateId should create new Id for new name")]
    public void MCSC001()
    {
        var name = McpServerName.Create("chronos").Value;

        var id = _cache.GetOrCreateId(name);

        id.Should().NotBeNull();
        Guid.TryParse(id.Value, out _).Should().BeTrue();
    }

    [Fact(DisplayName = "MCSC-002: GetOrCreateId should return same Id for same name")]
    public void MCSC002()
    {
        var name = McpServerName.Create("chronos").Value;

        var id1 = _cache.GetOrCreateId(name);
        var id2 = _cache.GetOrCreateId(name);

        id1.Should().Be(id2);
    }

    [Fact(DisplayName = "MCSC-003: GetOrCreateId should return different Ids for different names")]
    public void MCSC003()
    {
        var name1 = McpServerName.Create("chronos").Value;
        var name2 = McpServerName.Create("github").Value;

        var id1 = _cache.GetOrCreateId(name1);
        var id2 = _cache.GetOrCreateId(name2);

        id1.Should().NotBe(id2);
    }

    [Fact(DisplayName = "MCSC-004: GetEntry should return Unknown status for uncached Id")]
    public void MCSC004()
    {
        var id = McpServerId.Create();

        var entry = _cache.GetEntry(id);

        entry.Status.Should().Be(McpServerConnectionStatus.Unknown);
        entry.UpdatedOnUtc.Should().BeNull();
    }

    [Fact(DisplayName = "MCSC-005: SetStatus should cache status with timestamp")]
    public void MCSC005()
    {
        var id = McpServerId.Create();
        var beforeSet = DateTime.UtcNow;

        _cache.SetStatus(id, McpServerConnectionStatus.Verified);

        var entry = _cache.GetEntry(id);
        entry.Status.Should().Be(McpServerConnectionStatus.Verified);
        entry.UpdatedOnUtc.Should().NotBeNull();
        entry.UpdatedOnUtc.Should().BeOnOrAfter(beforeSet);
    }

    [Fact(DisplayName = "MCSC-006: SetStatus should update existing status")]
    public void MCSC006()
    {
        var id = McpServerId.Create();
        _cache.SetStatus(id, McpServerConnectionStatus.Verified);

        _cache.SetStatus(id, McpServerConnectionStatus.Failed);

        var entry = _cache.GetEntry(id);
        entry.Status.Should().Be(McpServerConnectionStatus.Failed);
    }

    [Fact(DisplayName = "MCSC-007: RemoveByName should clear status but preserve mapping for audit trail")]
    public void MCSC007()
    {
        var name = McpServerName.Create("chronos").Value;
        var id = _cache.GetOrCreateId(name);
        _cache.SetStatus(id, McpServerConnectionStatus.Verified);

        _cache.RemoveByName(name);

        // After removal, GetOrCreateId should return the same Id (mapping preserved)
        var sameId = _cache.GetOrCreateId(name);
        sameId.Should().Be(id);

        // Status should be Unknown (cleared)
        var entry = _cache.GetEntry(sameId);
        entry.Status.Should().Be(McpServerConnectionStatus.Unknown);
    }

    [Fact(DisplayName = "MCSC-008: RemoveByName should not throw for non-existent name")]
    public void MCSC008()
    {
        var name = McpServerName.Create("non-existent").Value;

        var action = () => _cache.RemoveByName(name);

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "MCSC-009: Cache should be thread-safe for concurrent GetOrCreateId")]
    public void MCSC009()
    {
        var name = McpServerName.Create("concurrent-server").Value;
        var ids = new List<McpServerId>();
        var lockObj = new object();

        Parallel.For(0, 100, _ =>
        {
            var id = _cache.GetOrCreateId(name);
            lock (lockObj)
            {
                ids.Add(id);
            }
        });

        // All IDs should be the same
        ids.Should().AllBeEquivalentTo(ids[0]);
    }

    [Fact(DisplayName = "MCSC-010: Cache should be thread-safe for concurrent SetStatus")]
    public void MCSC010()
    {
        var id = McpServerId.Create();

        var action = () => Parallel.For(0, 100, i =>
        {
            var status = i % 2 == 0 ? McpServerConnectionStatus.Verified : McpServerConnectionStatus.Failed;
            _cache.SetStatus(id, status);
        });

        action.Should().NotThrow();

        // Final status should be either Verified or Failed (not Unknown)
        var entry = _cache.GetEntry(id);
        entry.Status.Should().NotBe(McpServerConnectionStatus.Unknown);
    }

    [Fact(DisplayName = "MCSC-017: SetMetadata should cache metadata")]
    public void MCSC017()
    {
        var id = McpServerId.Create();
        var metadata = new McpServerMetadata(
            new List<McpTool> { new("test-tool", "Test Tool", "A test tool", "{}") },
            new List<McpPrompt> { new("test-prompt", "Test Prompt", "A test prompt", null) },
            new List<McpResource> { new("test-resource", "file://test", "Test Resource", "A test resource", "text/plain") },
            DateTime.UtcNow,
            null);

        _cache.SetMetadata(id, metadata);

        var cached = _cache.GetMetadata(id);
        cached.Should().NotBeNull();
        cached!.Tools.Should().HaveCount(1);
        cached.Prompts.Should().HaveCount(1);
        cached.Resources.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MCSC-018: GetMetadata should return null for uncached Id")]
    public void MCSC018()
    {
        var id = McpServerId.Create();

        var metadata = _cache.GetMetadata(id);

        metadata.Should().BeNull();
    }

    [Fact(DisplayName = "MCSC-019: SetMetadata should update existing metadata")]
    public void MCSC019()
    {
        var id = McpServerId.Create();
        var metadata1 = new McpServerMetadata(
            new List<McpTool> { new("tool1", "Tool 1", null, null) },
            null, null, DateTime.UtcNow, null);

        _cache.SetMetadata(id, metadata1);

        var metadata2 = new McpServerMetadata(
            new List<McpTool> { new("tool2", "Tool 2", null, null), new("tool3", "Tool 3", null, null) },
            null, null, DateTime.UtcNow, null);

        _cache.SetMetadata(id, metadata2);

        var cached = _cache.GetMetadata(id);
        cached.Should().NotBeNull();
        cached!.Tools.Should().HaveCount(2);
        cached.Tools![0].Name.Should().Be("tool2");
    }

    [Fact(DisplayName = "MCSC-020: RemoveByName should clear metadata")]
    public void MCSC020()
    {
        var name = McpServerName.Create("test-server").Value;
        var id = _cache.GetOrCreateId(name);
        var metadata = new McpServerMetadata(
            new List<McpTool> { new("test-tool", null, null, null) },
            null, null, DateTime.UtcNow, null);
        _cache.SetMetadata(id, metadata);

        _cache.RemoveByName(name);

        var cached = _cache.GetMetadata(id);
        cached.Should().BeNull();
    }

    [Fact(DisplayName = "MCSC-021: SetMetadata should handle metadata with errors")]
    public void MCSC021()
    {
        var id = McpServerId.Create();
        var errors = new List<McpServerMetadataError>
        {
            new("Tools", "Failed to retrieve tools"),
            new("Prompts", "Failed to retrieve prompts")
        };
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, errors);

        _cache.SetMetadata(id, metadata);

        var cached = _cache.GetMetadata(id);
        cached.Should().NotBeNull();
        cached!.RetrievalErrors.Should().HaveCount(2);
        cached.RetrievalErrors![0].Category.Should().Be("Tools");
    }
}
