using Core.Domain.McpServers;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory;

public class GlobalEventStoreTests
{
    private readonly GlobalEventStore _store;

    public GlobalEventStoreTests()
    {
        _store = new GlobalEventStore();
    }

    [Fact(DisplayName = "GES-001: RecordEvent should store event with timestamp")]
    public void GES001()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var beforeRecord = DateTime.UtcNow;

        _store.RecordEvent(GlobalEventType.ServerCreated, serverName);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging);
        result.Items.Should().HaveCount(1);
        result.Items[0].EventType.Should().Be(GlobalEventType.ServerCreated);
        result.Items[0].ServerName.Should().Be(serverName);
        result.Items[0].TimestampUtc.Should().BeOnOrAfter(beforeRecord);
    }

    [Fact(DisplayName = "GES-002: GetEvents should return empty result for empty store")]
    public void GES002()
    {
        var paging = PagingParameters.Create(1, 10).Value;

        var result = _store.GetEvents(paging);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }

    [Fact(DisplayName = "GES-003: GetEvents should filter by server name")]
    public void GES003()
    {
        var chronos = McpServerName.Create("chronos").Value;
        var github = McpServerName.Create("github").Value;
        _store.RecordEvent(GlobalEventType.ServerCreated, chronos);
        _store.RecordEvent(GlobalEventType.ServerCreated, github);
        _store.RecordEvent(GlobalEventType.ServerDeleted, chronos);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, serverNameFilter: chronos);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.ServerName.Should().Be(chronos));
        result.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "GES-004: GetEvents should filter by date range")]
    public void GES004()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.RecordEvent(GlobalEventType.ServerCreated, serverName);
        Thread.Sleep(50);
        var afterFirstEvent = DateTime.UtcNow;
        Thread.Sleep(50);
        _store.RecordEvent(GlobalEventType.ServerDeleted, serverName);

        var paging = PagingParameters.Create(1, 10).Value;
        var dateFilter = new DateRangeFilter(afterFirstEvent, null);
        var result = _store.GetEvents(paging, dateFilter: dateFilter);

        result.Items.Should().HaveCount(1);
        result.Items[0].EventType.Should().Be(GlobalEventType.ServerDeleted);
    }

    [Fact(DisplayName = "GES-005: GetEvents should sort ascending by timestamp")]
    public void GES005()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.RecordEvent(GlobalEventType.ServerCreated, serverName);
        Thread.Sleep(10);
        _store.RecordEvent(GlobalEventType.ServerDeleted, serverName);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging, sortDirection: SortDirection.Ascending);

        result.Items.Should().HaveCount(2);
        result.Items[0].EventType.Should().Be(GlobalEventType.ServerCreated);
        result.Items[1].EventType.Should().Be(GlobalEventType.ServerDeleted);
    }

    [Fact(DisplayName = "GES-006: GetEvents should sort descending by timestamp by default")]
    public void GES006()
    {
        var serverName = McpServerName.Create("chronos").Value;
        _store.RecordEvent(GlobalEventType.ServerCreated, serverName);
        Thread.Sleep(10);
        _store.RecordEvent(GlobalEventType.ServerDeleted, serverName);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetEvents(paging);

        result.Items.Should().HaveCount(2);
        result.Items[0].EventType.Should().Be(GlobalEventType.ServerDeleted);
        result.Items[1].EventType.Should().Be(GlobalEventType.ServerCreated);
    }

    [Fact(DisplayName = "GES-007: GetEvents should paginate results")]
    public void GES007()
    {
        var server1 = McpServerName.Create("server1").Value;
        var server2 = McpServerName.Create("server2").Value;
        var server3 = McpServerName.Create("server3").Value;
        _store.RecordEvent(GlobalEventType.ServerCreated, server1);
        _store.RecordEvent(GlobalEventType.ServerCreated, server2);
        _store.RecordEvent(GlobalEventType.ServerCreated, server3);

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

    [Fact(DisplayName = "GES-008: Store should be thread-safe for concurrent RecordEvent")]
    public void GES008()
    {
        var serverName = McpServerName.Create("concurrent-server").Value;

        var action = () => Parallel.For(0, 100, i =>
        {
            var eventType = i % 2 == 0 ? GlobalEventType.ServerCreated : GlobalEventType.ServerDeleted;
            _store.RecordEvent(eventType, serverName);
        });

        action.Should().NotThrow();

        var paging = PagingParameters.Create(1, 100).Value;
        var result = _store.GetEvents(paging);
        result.TotalItems.Should().Be(100);
    }

    [Fact(DisplayName = "GES-009: Store should be thread-safe for concurrent GetEvents")]
    public void GES009()
    {
        var serverName = McpServerName.Create("test-server").Value;
        for (int i = 0; i < 50; i++)
        {
            _store.RecordEvent(GlobalEventType.ServerCreated, serverName);
        }

        var action = () => Parallel.For(0, 100, _ =>
        {
            var paging = PagingParameters.Create(1, 100).Value;
            var result = _store.GetEvents(paging);
            result.Items.Should().HaveCount(50);
        });

        action.Should().NotThrow();
    }
}
