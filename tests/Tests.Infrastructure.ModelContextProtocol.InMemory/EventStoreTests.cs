using Core.Domain.Events;
using Core.Domain.Paging;
using Core.Infrastructure.Messaging.InMemory;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory;

public class EventStoreTests
{
    private readonly EventStore _store;

    public EventStoreTests()
    {
        _store = new EventStore();
    }

    [Fact(DisplayName = "EST-001: Store should store event with timestamp")]
    public void EST001()
    {
        var beforeRecord = DateTime.UtcNow;
        var @event = new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow);

        _store.Store(@event);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging);
        result.Items.Should().HaveCount(1);
        result.Items[0].Type.Should().Be(EventTypes.McpServer.Created);
        result.Items[0].Target.Should().Be("mcp-servers/chronos");
        result.Items[0].TimestampUtc.Should().BeOnOrAfter(beforeRecord);
    }

    [Fact(DisplayName = "EST-002: GetEvents should return empty result for empty store")]
    public void EST002()
    {
        var paging = PagingParameters.Create(1, 10).Value;

        var result = _store.GetEvents(paging);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }

    [Fact(DisplayName = "EST-003: GetEvents should filter by target prefix")]
    public void EST003()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/github", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Deleted, "mcp-servers/chronos", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, targetFilter: "mcp-servers/chronos");

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.Target.Should().StartWith("mcp-servers/chronos"));
        result.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "EST-004: GetEvents should filter by date range")]
    public void EST004()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow));
        Thread.Sleep(50);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(50);
        _store.Store(new Event(EventTypes.McpServer.Deleted, "mcp-servers/chronos", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var dateFilter = new DateRangeFilter(afterFirstEvent, null);
        var result = _store.GetEvents(paging, dateFilter: dateFilter);

        result.Items.Should().HaveCount(1);
        result.Items[0].Type.Should().Be(EventTypes.McpServer.Deleted);
    }

    [Fact(DisplayName = "EST-005: GetEvents should sort ascending by timestamp")]
    public void EST005()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new Event(EventTypes.McpServer.Deleted, "mcp-servers/chronos", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, sortDirection: SortDirection.Ascending);

        result.Items.Should().HaveCount(2);
        result.Items[0].Type.Should().Be(EventTypes.McpServer.Created);
        result.Items[1].Type.Should().Be(EventTypes.McpServer.Deleted);
    }

    [Fact(DisplayName = "EST-006: GetEvents should sort descending by timestamp by default")]
    public void EST006()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new Event(EventTypes.McpServer.Deleted, "mcp-servers/chronos", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging);

        result.Items.Should().HaveCount(2);
        result.Items[0].Type.Should().Be(EventTypes.McpServer.Deleted);
        result.Items[1].Type.Should().Be(EventTypes.McpServer.Created);
    }

    [Fact(DisplayName = "EST-007: GetEvents should paginate results")]
    public void EST007()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/server1", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/server2", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/server3", DateTime.UtcNow));

        var pagingPage1 = PagingParameters.Create(1, 2).Value;
        var pagingPage2 = PagingParameters.Create(2, 2).Value;

        var resultPage1 = _store.GetEvents(pagingPage1, sortDirection: SortDirection.Ascending);
        var resultPage2 = _store.GetEvents(pagingPage2, sortDirection: SortDirection.Ascending);

        resultPage1.Items.Should().HaveCount(2);
        resultPage1.TotalItems.Should().Be(3);
        resultPage1.TotalPages.Should().Be(2);
        resultPage1.Page.Should().Be(1);

        resultPage2.Items.Should().HaveCount(1);
        resultPage2.TotalItems.Should().Be(3);
        resultPage2.Page.Should().Be(2);
    }

    [Fact(DisplayName = "EST-008: Store should be thread-safe for concurrent Store")]
    public void EST008()
    {
        var action = () => Parallel.For(0, 100, i =>
        {
            var eventType = i % 2 == 0 ? EventTypes.McpServer.Created : EventTypes.McpServer.Deleted;
            _store.Store(new Event(eventType, "mcp-servers/concurrent-server", DateTime.UtcNow));
        });

        action.Should().NotThrow();

        var paging = PagingParameters.Create(1, 100).Value;
        var result = _store.GetEvents(paging);
        result.TotalItems.Should().Be(100);
    }

    [Fact(DisplayName = "EST-009: Store should be thread-safe for concurrent GetEvents")]
    public void EST009()
    {
        for (int i = 0; i < 50; i++)
        {
            _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/test-server", DateTime.UtcNow));
        }

        var action = () => Parallel.For(0, 100, _ =>
        {
            var paging = PagingParameters.Create(1, 100).Value;
            var result = _store.GetEvents(paging);
            result.Items.Should().HaveCount(50);
        });

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "EST-010: GetEvents should filter by target prefix including child targets")]
    public void EST010()
    {
        var instanceId1 = Guid.NewGuid().ToString();
        var instanceId2 = Guid.NewGuid().ToString();

        _store.Store(new Event(EventTypes.McpServer.Instance.Started, $"mcp-servers/chronos/instances/{instanceId1}", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, $"mcp-servers/chronos/instances/{instanceId2}", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Stopped, $"mcp-servers/chronos/instances/{instanceId1}", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, targetFilter: $"mcp-servers/chronos/instances/{instanceId1}");

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.Target.Should().Contain(instanceId1));
    }

    [Fact(DisplayName = "EST-011: GetEvents should filter by request ID")]
    public void EST011()
    {
        var requestId1 = Guid.NewGuid().ToString();
        var requestId2 = Guid.NewGuid().ToString();

        _store.Store(new Event(EventTypes.McpServer.ToolInvocation.Invoking, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow, null, requestId1));
        _store.Store(new Event(EventTypes.McpServer.ToolInvocation.Invoking, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow, null, requestId2));
        _store.Store(new Event(EventTypes.McpServer.ToolInvocation.Invoked, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow, null, requestId1));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, requestIdFilter: requestId1);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.RequestId.Should().Be(requestId1));
    }

    [Fact(DisplayName = "EST-012: GetEvents should combine multiple filters")]
    public void EST012()
    {
        var instanceId = Guid.NewGuid().ToString();

        _store.Store(new Event(EventTypes.McpServer.Instance.Started, $"mcp-servers/chronos/instances/{instanceId}", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Stopped, $"mcp-servers/chronos/instances/{instanceId}", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, $"mcp-servers/github/instances/{instanceId}", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, targetFilter: $"mcp-servers/chronos/instances/{instanceId}");

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e =>
        {
            e.Target.Should().Contain("chronos");
            e.Target.Should().Contain(instanceId);
        });
    }

    [Fact(DisplayName = "EST-013: GetEventsAfter should return events after timestamp")]
    public void EST013()
    {
        var timestamp1 = DateTime.UtcNow;
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", timestamp1));
        Thread.Sleep(10);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(10);
        var timestamp2 = DateTime.UtcNow;
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", timestamp2));
        Thread.Sleep(10);
        var timestamp3 = DateTime.UtcNow;
        _store.Store(new Event(EventTypes.McpServer.Instance.Stopped, "mcp-servers/chronos", timestamp3));

        var result = _store.GetEventsAfter(afterFirstEvent).ToList();

        result.Should().HaveCount(2);
        result[0].Type.Should().Be(EventTypes.McpServer.Instance.Started);
        result[1].Type.Should().Be(EventTypes.McpServer.Instance.Stopped);
    }

    [Fact(DisplayName = "EST-014: GetEventsAfter should return empty for future timestamp")]
    public void EST014()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow));

        var futureTimestamp = DateTime.UtcNow.AddHours(1);
        var result = _store.GetEventsAfter(futureTimestamp).ToList();

        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "EST-015: GetEventsAfter should filter by target prefix")]
    public void EST015()
    {
        var baseTime = DateTime.UtcNow;
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", baseTime));
        Thread.Sleep(10);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(10);
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/github/instances/inst2", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Stopped, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow));

        var result = _store.GetEventsAfter(afterFirstEvent, "mcp-servers/chronos").ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Target.Should().StartWith("mcp-servers/chronos"));
    }

    [Fact(DisplayName = "EST-016: GetEventsAfter should return events in ascending order")]
    public void EST016()
    {
        var baseTime = DateTime.UtcNow;

        Thread.Sleep(10);
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new Event(EventTypes.McpServer.Instance.Stopped, "mcp-servers/chronos", DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new Event(EventTypes.McpServer.Instance.Starting, "mcp-servers/chronos", DateTime.UtcNow));

        var result = _store.GetEventsAfter(baseTime).ToList();

        result.Should().HaveCount(3);
        result[0].Type.Should().Be(EventTypes.McpServer.Instance.Started);
        result[1].Type.Should().Be(EventTypes.McpServer.Instance.Stopped);
        result[2].Type.Should().Be(EventTypes.McpServer.Instance.Starting);
    }

    [Fact(DisplayName = "EST-017: GetEventsAfter should exclude event at exact timestamp")]
    public void EST017()
    {
        var exactTimestamp = new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", exactTimestamp));
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos", exactTimestamp.AddSeconds(1)));

        var result = _store.GetEventsAfter(exactTimestamp).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(EventTypes.McpServer.Instance.Started);
    }

    [Fact(DisplayName = "EST-018: GetEvents should filter by event type")]
    public void EST018()
    {
        _store.Store(new Event(EventTypes.McpServer.Created, "mcp-servers/chronos", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Started, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow));
        _store.Store(new Event(EventTypes.McpServer.Instance.Stopped, "mcp-servers/chronos/instances/inst1", DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, eventTypeFilter: EventTypes.McpServer.Instance.Started);

        result.Items.Should().HaveCount(1);
        result.Items[0].Type.Should().Be(EventTypes.McpServer.Instance.Started);
    }
}
