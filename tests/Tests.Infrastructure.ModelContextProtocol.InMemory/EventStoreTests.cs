using Core.Domain.McpServers;
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
        var serverName = McpServerName.Create("chronos").Value;
        var beforeRecord = DateTime.UtcNow;
        var @event = new McpServerEvent(serverName, McpServerEventType.ServerCreated, DateTime.UtcNow);

        _store.Store(@event);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging);
        result.Items.Should().HaveCount(1);
        result.Items[0].EventType.Should().Be(McpServerEventType.ServerCreated);
        result.Items[0].ServerName.Should().Be(serverName);
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

    [Fact(DisplayName = "EST-003: GetEvents should filter by server name")]
    public void EST003()
    {
        var chronos = McpServerName.Create("chronos").Value;
        var github = McpServerName.Create("github").Value;
        _store.Store(new McpServerEvent(chronos, McpServerEventType.ServerCreated, DateTime.UtcNow));
        _store.Store(new McpServerEvent(github, McpServerEventType.ServerCreated, DateTime.UtcNow));
        _store.Store(new McpServerEvent(chronos, McpServerEventType.ServerDeleted, DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, serverNameFilter: chronos);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.ServerName.Should().Be(chronos));
        result.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "EST-004: GetEvents should filter by date range")]
    public void EST004()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, DateTime.UtcNow));
        Thread.Sleep(50);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(50);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerDeleted, DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var dateFilter = new DateRangeFilter(afterFirstEvent, null);
        var result = _store.GetEvents(paging, dateFilter: dateFilter);

        result.Items.Should().HaveCount(1);
        result.Items[0].EventType.Should().Be(McpServerEventType.ServerDeleted);
    }

    [Fact(DisplayName = "EST-005: GetEvents should sort ascending by timestamp")]
    public void EST005()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerDeleted, DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, sortDirection: SortDirection.Ascending);

        result.Items.Should().HaveCount(2);
        result.Items[0].EventType.Should().Be(McpServerEventType.ServerCreated);
        result.Items[1].EventType.Should().Be(McpServerEventType.ServerDeleted);
    }

    [Fact(DisplayName = "EST-006: GetEvents should sort descending by timestamp by default")]
    public void EST006()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerDeleted, DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging);

        result.Items.Should().HaveCount(2);
        result.Items[0].EventType.Should().Be(McpServerEventType.ServerDeleted);
        result.Items[1].EventType.Should().Be(McpServerEventType.ServerCreated);
    }

    [Fact(DisplayName = "EST-007: GetEvents should paginate results")]
    public void EST007()
    {
        var server1 = McpServerName.Create("server1").Value;
        var server2 = McpServerName.Create("server2").Value;
        var server3 = McpServerName.Create("server3").Value;
        _store.Store(new McpServerEvent(server1, McpServerEventType.ServerCreated, DateTime.UtcNow));
        _store.Store(new McpServerEvent(server2, McpServerEventType.ServerCreated, DateTime.UtcNow));
        _store.Store(new McpServerEvent(server3, McpServerEventType.ServerCreated, DateTime.UtcNow));

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
        var serverName = McpServerName.Create("concurrent-server").Value;

        var action = () => Parallel.For(0, 100, i =>
        {
            var eventType = i % 2 == 0 ? McpServerEventType.ServerCreated : McpServerEventType.ServerDeleted;
            _store.Store(new McpServerEvent(serverName, eventType, DateTime.UtcNow));
        });

        action.Should().NotThrow();

        var paging = PagingParameters.Create(1, 100).Value;
        var result = _store.GetEvents(paging);
        result.TotalItems.Should().Be(100);
    }

    [Fact(DisplayName = "EST-009: Store should be thread-safe for concurrent GetEvents")]
    public void EST009()
    {
        var serverName = McpServerName.Create("test-server").Value;
        for (int i = 0; i < 50; i++)
        {
            _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, DateTime.UtcNow));
        }

        var action = () => Parallel.For(0, 100, _ =>
        {
            var paging = PagingParameters.Create(1, 100).Value;
            var result = _store.GetEvents(paging);
            result.Items.Should().HaveCount(50);
        });

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "EST-010: GetEvents should filter by instance ID")]
    public void EST010()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId1 = McpServerInstanceId.Create();
        var instanceId2 = McpServerInstanceId.Create();

        _store.Store(new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow, null, instanceId1));
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow, null, instanceId2));
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Stopped, DateTime.UtcNow, null, instanceId1));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, instanceIdFilter: instanceId1);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.InstanceId.Should().Be(instanceId1));
    }

    [Fact(DisplayName = "EST-011: GetEvents should filter by request ID")]
    public void EST011()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var requestId1 = McpServerRequestId.Create();
        var requestId2 = McpServerRequestId.Create();

        _store.Store(new McpServerEvent(serverName, McpServerEventType.ToolInvoking, DateTime.UtcNow, null, null, requestId1));
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ToolInvoking, DateTime.UtcNow, null, null, requestId2));
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ToolInvoked, DateTime.UtcNow, null, null, requestId1));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, requestIdFilter: requestId1);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.RequestId.Should().Be(requestId1));
    }

    [Fact(DisplayName = "EST-012: GetEvents should combine multiple filters")]
    public void EST012()
    {
        var chronos = McpServerName.Create("chronos").Value;
        var github = McpServerName.Create("github").Value;
        var instanceId = McpServerInstanceId.Create();

        _store.Store(new McpServerEvent(chronos, McpServerEventType.Started, DateTime.UtcNow, null, instanceId));
        _store.Store(new McpServerEvent(chronos, McpServerEventType.Stopped, DateTime.UtcNow, null, instanceId));
        _store.Store(new McpServerEvent(github, McpServerEventType.Started, DateTime.UtcNow, null, instanceId));
        _store.Store(new McpServerEvent(chronos, McpServerEventType.Started, DateTime.UtcNow));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, serverNameFilter: chronos, instanceIdFilter: instanceId);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e =>
        {
            e.ServerName.Should().Be(chronos);
            e.InstanceId.Should().Be(instanceId);
        });
    }

    [Fact(DisplayName = "EST-013: GetEventsAfter should return events after timestamp")]
    public void EST013()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var timestamp1 = DateTime.UtcNow;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, timestamp1));
        Thread.Sleep(10);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(10);
        var timestamp2 = DateTime.UtcNow;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Started, timestamp2));
        Thread.Sleep(10);
        var timestamp3 = DateTime.UtcNow;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Stopped, timestamp3));

        var result = _store.GetEventsAfter(afterFirstEvent).ToList();

        result.Should().HaveCount(2);
        result[0].EventType.Should().Be(McpServerEventType.Started);
        result[1].EventType.Should().Be(McpServerEventType.Stopped);
    }

    [Fact(DisplayName = "EST-014: GetEventsAfter should return empty for future timestamp")]
    public void EST014()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, DateTime.UtcNow));

        var futureTimestamp = DateTime.UtcNow.AddHours(1);
        var result = _store.GetEventsAfter(futureTimestamp).ToList();

        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "EST-015: GetEventsAfter should filter by server name")]
    public void EST015()
    {
        var chronos = McpServerName.Create("chronos").Value;
        var github = McpServerName.Create("github").Value;
        var baseTime = DateTime.UtcNow;

        _store.Store(new McpServerEvent(chronos, McpServerEventType.ServerCreated, baseTime));
        Thread.Sleep(10);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(10);
        _store.Store(new McpServerEvent(chronos, McpServerEventType.Started, DateTime.UtcNow));
        _store.Store(new McpServerEvent(github, McpServerEventType.Started, DateTime.UtcNow));
        _store.Store(new McpServerEvent(chronos, McpServerEventType.Stopped, DateTime.UtcNow));

        var result = _store.GetEventsAfter(afterFirstEvent, chronos).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.ServerName.Should().Be(chronos));
    }

    [Fact(DisplayName = "EST-016: GetEventsAfter should return events in ascending order")]
    public void EST016()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var baseTime = DateTime.UtcNow;

        Thread.Sleep(10);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Stopped, DateTime.UtcNow));
        Thread.Sleep(10);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Starting, DateTime.UtcNow));

        var result = _store.GetEventsAfter(baseTime).ToList();

        result.Should().HaveCount(3);
        result[0].EventType.Should().Be(McpServerEventType.Started);
        result[1].EventType.Should().Be(McpServerEventType.Stopped);
        result[2].EventType.Should().Be(McpServerEventType.Starting);
    }

    [Fact(DisplayName = "EST-017: GetEventsAfter should exclude event at exact timestamp")]
    public void EST017()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var exactTimestamp = new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);
        _store.Store(new McpServerEvent(serverName, McpServerEventType.ServerCreated, exactTimestamp));
        _store.Store(new McpServerEvent(serverName, McpServerEventType.Started, exactTimestamp.AddSeconds(1)));

        var result = _store.GetEventsAfter(exactTimestamp).ToList();

        result.Should().HaveCount(1);
        result[0].EventType.Should().Be(McpServerEventType.Started);
    }
}
