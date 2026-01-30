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

    [Fact(DisplayName = "MCSC-007: RemoveByName should remove both mapping and status")]
    public void MCSC007()
    {
        var name = McpServerName.Create("chronos").Value;
        var id = _cache.GetOrCreateId(name);
        _cache.SetStatus(id, McpServerConnectionStatus.Verified);

        _cache.RemoveByName(name);

        // After removal, GetOrCreateId should create a new Id
        var newId = _cache.GetOrCreateId(name);
        newId.Should().NotBe(id);

        // Status should be Unknown for the new Id
        var entry = _cache.GetEntry(newId);
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

    [Fact(DisplayName = "MCSC-015: RemoveByName should also clear events")]
    public void MCSC015()
    {
        var name = McpServerName.Create("test-server").Value;
        var id = _cache.GetOrCreateId(name);
        _cache.RecordEvent(id, McpServerEventType.Starting);
        _cache.RecordEvent(id, McpServerEventType.Started);

        _cache.RemoveByName(name);

        // After removal, events should be gone for old id
        var events = _cache.GetEvents(id);
        events.Should().BeEmpty();
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
}
