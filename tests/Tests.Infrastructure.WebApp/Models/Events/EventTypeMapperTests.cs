using Core.Domain.Events;
using Core.Infrastructure.WebApp.Models.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.Events;

public class EventTypeMapperTests
{
    [Fact(DisplayName = "ETM-001: ToListResponse should return all event types")]
    public void ETM001()
    {
        var result = EventTypeMapper.ToListResponse();

        // 24 event types total (2 server + 3 config + 6 lifecycle + 9 metadata + 4 tool invocation)
        result.Items.Should().HaveCount(24);
    }

    [Fact(DisplayName = "ETM-002: ToListResponse should return items with correct properties")]
    public void ETM002()
    {
        var result = EventTypeMapper.ToListResponse();

        foreach (var item in result.Items)
        {
            item.Name.Should().NotBeNullOrEmpty();
            item.Category.Should().NotBeNullOrEmpty();
            item.Description.Should().NotBeNullOrEmpty();
        }
    }

    [Fact(DisplayName = "ETM-003: ToListResponse should map mcp-server.instance.starting event correctly")]
    public void ETM003()
    {
        var result = EventTypeMapper.ToListResponse();

        var startingEvent = result.Items.Single(e => e.Name == EventTypes.McpServer.Instance.Starting.Value);
        startingEvent.Category.Should().Be("lifecycle");
        startingEvent.Description.Should().Be("Server instance is starting.");
    }

    [Fact(DisplayName = "ETM-004: ToListResponse should map mcp-server.configuration.created event correctly")]
    public void ETM004()
    {
        var result = EventTypeMapper.ToListResponse();

        var configEvent = result.Items.Single(e => e.Name == EventTypes.McpServer.Configuration.Created.Value);
        configEvent.Category.Should().Be("configuration");
        configEvent.Description.Should().Be("Server configuration was created.");
    }

    [Fact(DisplayName = "ETM-005: ToListResponse should map mcp-server.metadata.tools.retrieving event correctly")]
    public void ETM005()
    {
        var result = EventTypeMapper.ToListResponse();

        var metadataEvent = result.Items.Single(e => e.Name == EventTypes.McpServer.Metadata.Tools.Retrieving.Value);
        metadataEvent.Category.Should().Be("metadata");
        metadataEvent.Description.Should().Be("Tools retrieval is starting.");
    }

    [Fact(DisplayName = "ETM-006: ToListResponse should map mcp-server.tool-invocation.accepted event correctly")]
    public void ETM006()
    {
        var result = EventTypeMapper.ToListResponse();

        var toolEvent = result.Items.Single(e => e.Name == EventTypes.McpServer.ToolInvocation.Accepted.Value);
        toolEvent.Category.Should().Be("tool-invocation");
        toolEvent.Description.Should().Be("Tool invocation was accepted and queued.");
    }

    [Fact(DisplayName = "ETM-007: ToListResponse should categorize lifecycle events correctly")]
    public void ETM007()
    {
        var result = EventTypeMapper.ToListResponse();

        var lifecycleEvents = result.Items.Where(e => e.Category == "lifecycle").ToList();
        lifecycleEvents.Select(e => e.Name).Should().Contain(new[]
        {
            EventTypes.McpServer.Instance.Starting.Value,
            EventTypes.McpServer.Instance.Started.Value,
            EventTypes.McpServer.Instance.StartFailed.Value,
            EventTypes.McpServer.Instance.Stopping.Value,
            EventTypes.McpServer.Instance.Stopped.Value,
            EventTypes.McpServer.Instance.StopFailed.Value,
            EventTypes.McpServer.Created.Value,
            EventTypes.McpServer.Deleted.Value
        });
    }

    [Fact(DisplayName = "ETM-008: ToListResponse should categorize configuration events correctly")]
    public void ETM008()
    {
        var result = EventTypeMapper.ToListResponse();

        var configEvents = result.Items.Where(e => e.Category == "configuration").ToList();
        configEvents.Select(e => e.Name).Should().BeEquivalentTo(new[]
        {
            EventTypes.McpServer.Configuration.Created.Value,
            EventTypes.McpServer.Configuration.Updated.Value,
            EventTypes.McpServer.Configuration.Deleted.Value
        });
    }

    [Fact(DisplayName = "ETM-009: ToListResponse should categorize metadata events correctly")]
    public void ETM009()
    {
        var result = EventTypeMapper.ToListResponse();

        var metadataEvents = result.Items.Where(e => e.Category == "metadata").ToList();
        metadataEvents.Select(e => e.Name).Should().BeEquivalentTo(new[]
        {
            EventTypes.McpServer.Metadata.Tools.Retrieving.Value,
            EventTypes.McpServer.Metadata.Tools.Retrieved.Value,
            EventTypes.McpServer.Metadata.Tools.RetrievalFailed.Value,
            EventTypes.McpServer.Metadata.Prompts.Retrieving.Value,
            EventTypes.McpServer.Metadata.Prompts.Retrieved.Value,
            EventTypes.McpServer.Metadata.Prompts.RetrievalFailed.Value,
            EventTypes.McpServer.Metadata.Resources.Retrieving.Value,
            EventTypes.McpServer.Metadata.Resources.Retrieved.Value,
            EventTypes.McpServer.Metadata.Resources.RetrievalFailed.Value
        });
    }

    [Fact(DisplayName = "ETM-010: ToListResponse should categorize tool invocation events correctly")]
    public void ETM010()
    {
        var result = EventTypeMapper.ToListResponse();

        var toolInvocationEvents = result.Items.Where(e => e.Category == "tool-invocation").ToList();
        toolInvocationEvents.Select(e => e.Name).Should().BeEquivalentTo(new[]
        {
            EventTypes.McpServer.ToolInvocation.Accepted.Value,
            EventTypes.McpServer.ToolInvocation.Invoking.Value,
            EventTypes.McpServer.ToolInvocation.Invoked.Value,
            EventTypes.McpServer.ToolInvocation.Failed.Value
        });
    }

    [Fact(DisplayName = "ETM-011: ToListResponse should return same instance on multiple calls")]
    public void ETM011()
    {
        var result1 = EventTypeMapper.ToListResponse();
        var result2 = EventTypeMapper.ToListResponse();

        result1.Items.Should().BeSameAs(result2.Items);
    }

    [Fact(DisplayName = "ETM-013: ToListResponse should have unique names for all event types")]
    public void ETM013()
    {
        var result = EventTypeMapper.ToListResponse();

        var names = result.Items.Select(e => e.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }
}
