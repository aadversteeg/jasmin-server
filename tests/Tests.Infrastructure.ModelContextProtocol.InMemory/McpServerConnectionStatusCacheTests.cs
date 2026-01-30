using System.Text.Json;
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

    [Fact(DisplayName = "MCSC-011: RecordEvent should store event with timestamp")]
    public void MCSC011()
    {
        var id = McpServerId.Create();
        var beforeRecord = DateTime.UtcNow;

        _cache.RecordEvent(id, McpServerEventType.Starting);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be(McpServerEventType.Starting);
        events[0].TimestampUtc.Should().BeOnOrAfter(beforeRecord);
        events[0].Errors.Should().BeNull();
    }

    [Fact(DisplayName = "MCSC-012: RecordEvent should store errors for failures")]
    public void MCSC012()
    {
        var id = McpServerId.Create();
        var errors = new List<McpServerEventError>
        {
            new("ConnectionError", "Connection refused")
        }.AsReadOnly();

        _cache.RecordEvent(id, McpServerEventType.StartFailed, errors);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be(McpServerEventType.StartFailed);
        events[0].Errors.Should().NotBeNull();
        events[0].Errors.Should().HaveCount(1);
        events[0].Errors![0].Code.Should().Be("ConnectionError");
        events[0].Errors[0].Message.Should().Be("Connection refused");
    }

    [Fact(DisplayName = "MCSC-013: GetEvents should return empty list for unknown Id")]
    public void MCSC013()
    {
        var id = McpServerId.Create();

        var events = _cache.GetEvents(id);

        events.Should().BeEmpty();
    }

    [Fact(DisplayName = "MCSC-014: GetEvents should return events ordered by timestamp")]
    public void MCSC014()
    {
        var id = McpServerId.Create();

        _cache.RecordEvent(id, McpServerEventType.Starting);
        Thread.Sleep(10); // Ensure different timestamps
        _cache.RecordEvent(id, McpServerEventType.Started);
        Thread.Sleep(10);
        _cache.RecordEvent(id, McpServerEventType.Stopping);
        Thread.Sleep(10);
        _cache.RecordEvent(id, McpServerEventType.Stopped);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(4);
        events[0].EventType.Should().Be(McpServerEventType.Starting);
        events[1].EventType.Should().Be(McpServerEventType.Started);
        events[2].EventType.Should().Be(McpServerEventType.Stopping);
        events[3].EventType.Should().Be(McpServerEventType.Stopped);

        // Verify ordering
        for (int i = 1; i < events.Count; i++)
        {
            events[i].TimestampUtc.Should().BeOnOrAfter(events[i - 1].TimestampUtc);
        }
    }

    [Fact(DisplayName = "MCSC-015: RemoveByName should preserve events for audit trail")]
    public void MCSC015()
    {
        var name = McpServerName.Create("test-server").Value;
        var id = _cache.GetOrCreateId(name);
        _cache.RecordEvent(id, McpServerEventType.Starting);
        _cache.RecordEvent(id, McpServerEventType.Started);

        _cache.RemoveByName(name);

        // After removal, events should still be available for audit trail
        var events = _cache.GetEvents(id);
        events.Should().HaveCount(2);
        events[0].EventType.Should().Be(McpServerEventType.Starting);
        events[1].EventType.Should().Be(McpServerEventType.Started);
    }

    [Fact(DisplayName = "MCSC-016: Cache should be thread-safe for concurrent RecordEvent")]
    public void MCSC016()
    {
        var id = McpServerId.Create();

        var action = () => Parallel.For(0, 100, i =>
        {
            var eventType = i % 2 == 0 ? McpServerEventType.Starting : McpServerEventType.Started;
            _cache.RecordEvent(id, eventType);
        });

        action.Should().NotThrow();

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(100);
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

    [Fact(DisplayName = "MCSC-022: RecordEvent should store tool invocation data")]
    public void MCSC022()
    {
        var id = McpServerId.Create();
        var instanceId = McpServerInstanceId.Create();
        var requestId = McpServerRequestId.Create();
        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var toolInvocationData = new McpServerToolInvocationEventData("get_time", input, null);

        _cache.RecordEvent(
            id,
            McpServerEventType.ToolInvoking,
            instanceId: instanceId,
            requestId: requestId,
            toolInvocationData: toolInvocationData);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be(McpServerEventType.ToolInvoking);
        events[0].InstanceId.Should().Be(instanceId);
        events[0].RequestId.Should().Be(requestId);
        events[0].ToolInvocationData.Should().NotBeNull();
        events[0].ToolInvocationData!.ToolName.Should().Be("get_time");
    }

    [Fact(DisplayName = "MCSC-023: RecordEvent should store ToolInvocationAccepted event")]
    public void MCSC023()
    {
        var id = McpServerId.Create();
        var instanceId = McpServerInstanceId.Create();
        var input = JsonSerializer.SerializeToElement(new { param = "value" });
        var toolInvocationData = new McpServerToolInvocationEventData("test_tool", input, null);

        _cache.RecordEvent(
            id,
            McpServerEventType.ToolInvocationAccepted,
            instanceId: instanceId,
            toolInvocationData: toolInvocationData);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be(McpServerEventType.ToolInvocationAccepted);
        events[0].ToolInvocationData!.ToolName.Should().Be("test_tool");
    }

    [Fact(DisplayName = "MCSC-024: RecordEvent should store ToolInvoked event with output")]
    public void MCSC024()
    {
        var id = McpServerId.Create();
        var instanceId = McpServerInstanceId.Create();
        var input = JsonSerializer.SerializeToElement(new { param = "value" });
        var output = JsonSerializer.SerializeToElement(new { result = "success" });
        var toolInvocationData = new McpServerToolInvocationEventData("test_tool", input, output);

        _cache.RecordEvent(
            id,
            McpServerEventType.ToolInvoked,
            instanceId: instanceId,
            toolInvocationData: toolInvocationData);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be(McpServerEventType.ToolInvoked);
        events[0].ToolInvocationData!.Output.Should().NotBeNull();
    }

    [Fact(DisplayName = "MCSC-025: RecordEvent should store ToolInvocationFailed event with errors")]
    public void MCSC025()
    {
        var id = McpServerId.Create();
        var instanceId = McpServerInstanceId.Create();
        var input = JsonSerializer.SerializeToElement(new { param = "value" });
        var toolInvocationData = new McpServerToolInvocationEventData("test_tool", input, null);
        var errors = new List<McpServerEventError>
        {
            new("ToolNotFound", "Tool 'test_tool' was not found")
        }.AsReadOnly();

        _cache.RecordEvent(
            id,
            McpServerEventType.ToolInvocationFailed,
            errors,
            instanceId: instanceId,
            toolInvocationData: toolInvocationData);

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be(McpServerEventType.ToolInvocationFailed);
        events[0].Errors.Should().NotBeNull();
        events[0].Errors.Should().HaveCount(1);
        events[0].ToolInvocationData!.ToolName.Should().Be("test_tool");
    }

    [Fact(DisplayName = "MCSC-026: Tool invocation events should maintain order")]
    public void MCSC026()
    {
        var id = McpServerId.Create();
        var instanceId = McpServerInstanceId.Create();
        var requestId = McpServerRequestId.Create();
        var input = JsonSerializer.SerializeToElement(new { param = "value" });
        var output = JsonSerializer.SerializeToElement(new { result = "done" });

        _cache.RecordEvent(id, McpServerEventType.ToolInvocationAccepted,
            instanceId: instanceId, requestId: requestId,
            toolInvocationData: new McpServerToolInvocationEventData("tool", input, null));
        Thread.Sleep(10);
        _cache.RecordEvent(id, McpServerEventType.ToolInvoking,
            instanceId: instanceId, requestId: requestId,
            toolInvocationData: new McpServerToolInvocationEventData("tool", input, null));
        Thread.Sleep(10);
        _cache.RecordEvent(id, McpServerEventType.ToolInvoked,
            instanceId: instanceId, requestId: requestId,
            toolInvocationData: new McpServerToolInvocationEventData("tool", input, output));

        var events = _cache.GetEvents(id);
        events.Should().HaveCount(3);
        events[0].EventType.Should().Be(McpServerEventType.ToolInvocationAccepted);
        events[1].EventType.Should().Be(McpServerEventType.ToolInvoking);
        events[2].EventType.Should().Be(McpServerEventType.ToolInvoked);
    }
}
