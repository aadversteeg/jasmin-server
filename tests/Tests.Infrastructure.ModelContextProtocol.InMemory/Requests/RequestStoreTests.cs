using Core.Domain.Paging;
using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.InMemory.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory.Requests;

public class RequestStoreTests
{
    private readonly RequestStore _store;

    public RequestStoreTests()
    {
        _store = new RequestStore();
    }

    [Fact(DisplayName = "RST-001: Add and retrieve by ID should return the request")]
    public void RST001()
    {
        var request = CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos");
        _store.Add(request);

        var result = _store.GetById(request.Id);

        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(request.Id);
        result.Value.Action.Should().Be(RequestActions.McpServer.Start);
    }

    [Fact(DisplayName = "RST-002: GetById should return None for unknown ID")]
    public void RST002()
    {
        var result = _store.GetById(RequestId.Create());

        result.HasValue.Should().BeFalse();
    }

    [Fact(DisplayName = "RST-003: GetAll should return paged results")]
    public void RST003()
    {
        for (int i = 0; i < 5; i++)
        {
            _store.Add(CreateRequest(RequestActions.McpServer.Start, $"mcp-servers/server{i}"));
        }

        var paging = PagingParameters.Create(1, 3).Value;
        var result = _store.GetAll(paging);

        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(5);
        result.TotalPages.Should().Be(2);
    }

    [Fact(DisplayName = "RST-004: GetAll should filter by target prefix")]
    public void RST004()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos"));
        _store.Add(CreateRequest(RequestActions.McpServer.Instance.InvokeTool, "mcp-servers/chronos/instances/abc"));
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/github"));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, targetFilter: "mcp-servers/chronos");

        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
    }

    [Fact(DisplayName = "RST-005: GetAll should filter by action")]
    public void RST005()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos"));
        _store.Add(CreateRequest(RequestActions.McpServer.Instance.InvokeTool, "mcp-servers/chronos/instances/abc"));
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/github"));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, actionFilter: RequestActions.McpServer.Start);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(r => r.Action.Should().Be(RequestActions.McpServer.Start));
    }

    [Fact(DisplayName = "RST-006: GetAll should filter by status")]
    public void RST006()
    {
        var request1 = CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos");
        var request2 = CreateRequest(RequestActions.McpServer.Start, "mcp-servers/github");
        request1.MarkRunning();
        _store.Add(request1);
        _store.Add(request2);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, statusFilter: RequestStatus.Running);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(request1.Id);
    }

    [Fact(DisplayName = "RST-007: GetAll should filter by date range")]
    public void RST007()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos"));
        Thread.Sleep(50);
        var afterFirst = DateTime.UtcNow;
        Thread.Sleep(50);
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/github"));

        var paging = PagingParameters.Create(1, 10).Value;
        var dateFilter = new DateRangeFilter(afterFirst, null);
        var result = _store.GetAll(paging, dateFilter: dateFilter);

        result.Items.Should().HaveCount(1);
    }

    [Fact(DisplayName = "RST-008: GetAll should order by createdAt descending by default")]
    public void RST008()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/first"));
        Thread.Sleep(10);
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/second"));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging);

        result.Items.Should().HaveCount(2);
        result.Items[0].Target.Should().Be("mcp-servers/second");
        result.Items[1].Target.Should().Be("mcp-servers/first");
    }

    [Fact(DisplayName = "RST-009: GetAll should order by createdAt ascending")]
    public void RST009()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/first"));
        Thread.Sleep(10);
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/second"));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, sortDirection: SortDirection.Ascending);

        result.Items.Should().HaveCount(2);
        result.Items[0].Target.Should().Be("mcp-servers/first");
        result.Items[1].Target.Should().Be("mcp-servers/second");
    }

    [Fact(DisplayName = "RST-010: GetAll should order by completedAt")]
    public void RST010()
    {
        var request1 = CreateRequest(RequestActions.McpServer.Start, "mcp-servers/first");
        var request2 = CreateRequest(RequestActions.McpServer.Start, "mcp-servers/second");
        request2.MarkRunning();
        request2.MarkCompleted();
        Thread.Sleep(10);
        request1.MarkRunning();
        request1.MarkCompleted();
        _store.Add(request1);
        _store.Add(request2);

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, orderBy: "completedAt", sortDirection: SortDirection.Ascending);

        result.Items.Should().HaveCount(2);
        result.Items[0].Target.Should().Be("mcp-servers/second");
        result.Items[1].Target.Should().Be("mcp-servers/first");
    }

    [Fact(DisplayName = "RST-011: Update should update existing request")]
    public void RST011()
    {
        var request = CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos");
        _store.Add(request);

        request.MarkRunning();
        _store.Update(request);

        var result = _store.GetById(request.Id);
        result.HasValue.Should().BeTrue();
        result.Value.Status.Should().Be(RequestStatus.Running);
    }

    [Fact(DisplayName = "RST-012: GetAll should match exact target")]
    public void RST012()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos"));
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos-extended"));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, targetFilter: "mcp-servers/chronos");

        result.Items.Should().HaveCount(1);
        result.Items[0].Target.Should().Be("mcp-servers/chronos");
    }

    [Fact(DisplayName = "RST-013: GetAll with target filter should exclude null-target requests")]
    public void RST013()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos"));
        _store.Add(new Request(RequestId.Create(), RequestActions.McpServer.TestConfiguration));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging, targetFilter: "mcp-servers/chronos");

        result.Items.Should().HaveCount(1);
        result.Items[0].Target.Should().Be("mcp-servers/chronos");
    }

    [Fact(DisplayName = "RST-014: GetAll without target filter should include null-target requests")]
    public void RST014()
    {
        _store.Add(CreateRequest(RequestActions.McpServer.Start, "mcp-servers/chronos"));
        _store.Add(new Request(RequestId.Create(), RequestActions.McpServer.TestConfiguration));

        var paging = PagingParameters.Create(1, 10).Value;
        var result = _store.GetAll(paging);

        result.Items.Should().HaveCount(2);
    }

    private static Request CreateRequest(RequestAction action, string target)
    {
        return new Request(RequestId.Create(), action, target);
    }
}
