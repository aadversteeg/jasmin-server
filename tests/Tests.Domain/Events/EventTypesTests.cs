using Core.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Events;

public class EventTypesTests
{
    [Fact(DisplayName = "ETS-001: McpServer.Created should be mcp-server.created")]
    public void ETS001()
    {
        EventTypes.McpServer.Created.Value.Should().Be("mcp-server.created");
    }

    [Fact(DisplayName = "ETS-002: McpServer.Deleted should be mcp-server.deleted")]
    public void ETS002()
    {
        EventTypes.McpServer.Deleted.Value.Should().Be("mcp-server.deleted");
    }

    [Fact(DisplayName = "ETS-003: Configuration.Created should be mcp-server.configuration.created")]
    public void ETS003()
    {
        EventTypes.McpServer.Configuration.Created.Value.Should().Be("mcp-server.configuration.created");
    }

    [Fact(DisplayName = "ETS-004: Configuration.Updated should be mcp-server.configuration.updated")]
    public void ETS004()
    {
        EventTypes.McpServer.Configuration.Updated.Value.Should().Be("mcp-server.configuration.updated");
    }

    [Fact(DisplayName = "ETS-005: Configuration.Deleted should be mcp-server.configuration.deleted")]
    public void ETS005()
    {
        EventTypes.McpServer.Configuration.Deleted.Value.Should().Be("mcp-server.configuration.deleted");
    }

    [Fact(DisplayName = "ETS-006: Instance.Starting should be mcp-server.instance.starting")]
    public void ETS006()
    {
        EventTypes.McpServer.Instance.Starting.Value.Should().Be("mcp-server.instance.starting");
    }

    [Fact(DisplayName = "ETS-007: Instance.Started should be mcp-server.instance.started")]
    public void ETS007()
    {
        EventTypes.McpServer.Instance.Started.Value.Should().Be("mcp-server.instance.started");
    }

    [Fact(DisplayName = "ETS-008: Instance.StartFailed should be mcp-server.instance.start-failed")]
    public void ETS008()
    {
        EventTypes.McpServer.Instance.StartFailed.Value.Should().Be("mcp-server.instance.start-failed");
    }

    [Fact(DisplayName = "ETS-009: Instance.Stopping should be mcp-server.instance.stopping")]
    public void ETS009()
    {
        EventTypes.McpServer.Instance.Stopping.Value.Should().Be("mcp-server.instance.stopping");
    }

    [Fact(DisplayName = "ETS-010: Instance.Stopped should be mcp-server.instance.stopped")]
    public void ETS010()
    {
        EventTypes.McpServer.Instance.Stopped.Value.Should().Be("mcp-server.instance.stopped");
    }

    [Fact(DisplayName = "ETS-011: Instance.StopFailed should be mcp-server.instance.stop-failed")]
    public void ETS011()
    {
        EventTypes.McpServer.Instance.StopFailed.Value.Should().Be("mcp-server.instance.stop-failed");
    }

    [Fact(DisplayName = "ETS-012: Metadata.Tools.Retrieving should be mcp-server.metadata.tools.retrieving")]
    public void ETS012()
    {
        EventTypes.McpServer.Metadata.Tools.Retrieving.Value.Should().Be("mcp-server.metadata.tools.retrieving");
    }

    [Fact(DisplayName = "ETS-013: Metadata.Tools.Retrieved should be mcp-server.metadata.tools.retrieved")]
    public void ETS013()
    {
        EventTypes.McpServer.Metadata.Tools.Retrieved.Value.Should().Be("mcp-server.metadata.tools.retrieved");
    }

    [Fact(DisplayName = "ETS-014: Metadata.Tools.RetrievalFailed should be mcp-server.metadata.tools.retrieval-failed")]
    public void ETS014()
    {
        EventTypes.McpServer.Metadata.Tools.RetrievalFailed.Value.Should().Be("mcp-server.metadata.tools.retrieval-failed");
    }

    [Fact(DisplayName = "ETS-015: Metadata.Prompts.Retrieving should be mcp-server.metadata.prompts.retrieving")]
    public void ETS015()
    {
        EventTypes.McpServer.Metadata.Prompts.Retrieving.Value.Should().Be("mcp-server.metadata.prompts.retrieving");
    }

    [Fact(DisplayName = "ETS-016: Metadata.Prompts.Retrieved should be mcp-server.metadata.prompts.retrieved")]
    public void ETS016()
    {
        EventTypes.McpServer.Metadata.Prompts.Retrieved.Value.Should().Be("mcp-server.metadata.prompts.retrieved");
    }

    [Fact(DisplayName = "ETS-017: Metadata.Prompts.RetrievalFailed should be mcp-server.metadata.prompts.retrieval-failed")]
    public void ETS017()
    {
        EventTypes.McpServer.Metadata.Prompts.RetrievalFailed.Value.Should().Be("mcp-server.metadata.prompts.retrieval-failed");
    }

    [Fact(DisplayName = "ETS-018: Metadata.Resources.Retrieving should be mcp-server.metadata.resources.retrieving")]
    public void ETS018()
    {
        EventTypes.McpServer.Metadata.Resources.Retrieving.Value.Should().Be("mcp-server.metadata.resources.retrieving");
    }

    [Fact(DisplayName = "ETS-019: Metadata.Resources.Retrieved should be mcp-server.metadata.resources.retrieved")]
    public void ETS019()
    {
        EventTypes.McpServer.Metadata.Resources.Retrieved.Value.Should().Be("mcp-server.metadata.resources.retrieved");
    }

    [Fact(DisplayName = "ETS-020: Metadata.Resources.RetrievalFailed should be mcp-server.metadata.resources.retrieval-failed")]
    public void ETS020()
    {
        EventTypes.McpServer.Metadata.Resources.RetrievalFailed.Value.Should().Be("mcp-server.metadata.resources.retrieval-failed");
    }

    [Fact(DisplayName = "ETS-021: ToolInvocation.Accepted should be mcp-server.tool-invocation.accepted")]
    public void ETS021()
    {
        EventTypes.McpServer.ToolInvocation.Accepted.Value.Should().Be("mcp-server.tool-invocation.accepted");
    }

    [Fact(DisplayName = "ETS-022: ToolInvocation.Invoking should be mcp-server.tool-invocation.invoking")]
    public void ETS022()
    {
        EventTypes.McpServer.ToolInvocation.Invoking.Value.Should().Be("mcp-server.tool-invocation.invoking");
    }

    [Fact(DisplayName = "ETS-023: ToolInvocation.Invoked should be mcp-server.tool-invocation.invoked")]
    public void ETS023()
    {
        EventTypes.McpServer.ToolInvocation.Invoked.Value.Should().Be("mcp-server.tool-invocation.invoked");
    }

    [Fact(DisplayName = "ETS-024: ToolInvocation.Failed should be mcp-server.tool-invocation.failed")]
    public void ETS024()
    {
        EventTypes.McpServer.ToolInvocation.Failed.Value.Should().Be("mcp-server.tool-invocation.failed");
    }

    [Fact(DisplayName = "ETS-025: All 24 event types should have unique values")]
    public void ETS025()
    {
        var allEventTypes = new[]
        {
            EventTypes.McpServer.Created,
            EventTypes.McpServer.Deleted,
            EventTypes.McpServer.Configuration.Created,
            EventTypes.McpServer.Configuration.Updated,
            EventTypes.McpServer.Configuration.Deleted,
            EventTypes.McpServer.Instance.Starting,
            EventTypes.McpServer.Instance.Started,
            EventTypes.McpServer.Instance.StartFailed,
            EventTypes.McpServer.Instance.Stopping,
            EventTypes.McpServer.Instance.Stopped,
            EventTypes.McpServer.Instance.StopFailed,
            EventTypes.McpServer.Metadata.Tools.Retrieving,
            EventTypes.McpServer.Metadata.Tools.Retrieved,
            EventTypes.McpServer.Metadata.Tools.RetrievalFailed,
            EventTypes.McpServer.Metadata.Prompts.Retrieving,
            EventTypes.McpServer.Metadata.Prompts.Retrieved,
            EventTypes.McpServer.Metadata.Prompts.RetrievalFailed,
            EventTypes.McpServer.Metadata.Resources.Retrieving,
            EventTypes.McpServer.Metadata.Resources.Retrieved,
            EventTypes.McpServer.Metadata.Resources.RetrievalFailed,
            EventTypes.McpServer.ToolInvocation.Accepted,
            EventTypes.McpServer.ToolInvocation.Invoking,
            EventTypes.McpServer.ToolInvocation.Invoked,
            EventTypes.McpServer.ToolInvocation.Failed
        };

        allEventTypes.Should().HaveCount(24);
        allEventTypes.Select(e => e.Value).Distinct().Should().HaveCount(24);
    }

    [Fact(DisplayName = "ETS-026: All event types should be children of mcp-server")]
    public void ETS026()
    {
        var mcpServer = new EventType("mcp-server");

        EventTypes.McpServer.Created.IsChildOf(mcpServer).Should().BeTrue();
        EventTypes.McpServer.Deleted.IsChildOf(mcpServer).Should().BeTrue();
        EventTypes.McpServer.Configuration.Created.IsChildOf(mcpServer).Should().BeTrue();
        EventTypes.McpServer.Instance.Started.IsChildOf(mcpServer).Should().BeTrue();
        EventTypes.McpServer.Metadata.Tools.Retrieved.IsChildOf(mcpServer).Should().BeTrue();
        EventTypes.McpServer.ToolInvocation.Accepted.IsChildOf(mcpServer).Should().BeTrue();
    }

    [Fact(DisplayName = "ETS-027: Instance events should be children of mcp-server.instance")]
    public void ETS027()
    {
        var instance = new EventType("mcp-server.instance");

        EventTypes.McpServer.Instance.Starting.IsChildOf(instance).Should().BeTrue();
        EventTypes.McpServer.Instance.Started.IsChildOf(instance).Should().BeTrue();
        EventTypes.McpServer.Instance.StartFailed.IsChildOf(instance).Should().BeTrue();
        EventTypes.McpServer.Instance.Stopping.IsChildOf(instance).Should().BeTrue();
        EventTypes.McpServer.Instance.Stopped.IsChildOf(instance).Should().BeTrue();
        EventTypes.McpServer.Instance.StopFailed.IsChildOf(instance).Should().BeTrue();
    }

    [Fact(DisplayName = "ETS-028: Metadata events should be children of mcp-server.metadata")]
    public void ETS028()
    {
        var metadata = new EventType("mcp-server.metadata");

        EventTypes.McpServer.Metadata.Tools.Retrieving.IsChildOf(metadata).Should().BeTrue();
        EventTypes.McpServer.Metadata.Prompts.Retrieving.IsChildOf(metadata).Should().BeTrue();
        EventTypes.McpServer.Metadata.Resources.Retrieving.IsChildOf(metadata).Should().BeTrue();
    }
}
