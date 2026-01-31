using Core.Domain.McpServers;
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

        var expectedCount = Enum.GetValues<McpServerEventType>().Length;
        result.Items.Should().HaveCount(expectedCount);
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

    [Fact(DisplayName = "ETM-003: ToListResponse should map Starting event correctly")]
    public void ETM003()
    {
        var result = EventTypeMapper.ToListResponse();

        var startingEvent = result.Items.Single(e => e.Name == "Starting");
        startingEvent.Value.Should().Be(0);
        startingEvent.Category.Should().Be("Lifecycle");
        startingEvent.Description.Should().Be("Server is attempting to start.");
    }

    [Fact(DisplayName = "ETM-004: ToListResponse should map ConfigurationCreated event correctly")]
    public void ETM004()
    {
        var result = EventTypeMapper.ToListResponse();

        var configEvent = result.Items.Single(e => e.Name == "ConfigurationCreated");
        configEvent.Value.Should().Be(6);
        configEvent.Category.Should().Be("Configuration");
        configEvent.Description.Should().Be("Server configuration was created.");
    }

    [Fact(DisplayName = "ETM-005: ToListResponse should map ToolsRetrieving event correctly")]
    public void ETM005()
    {
        var result = EventTypeMapper.ToListResponse();

        var metadataEvent = result.Items.Single(e => e.Name == "ToolsRetrieving");
        metadataEvent.Value.Should().Be(9);
        metadataEvent.Category.Should().Be("Metadata");
        metadataEvent.Description.Should().Be("Tools retrieval is starting.");
    }

    [Fact(DisplayName = "ETM-006: ToListResponse should map ToolInvocationAccepted event correctly")]
    public void ETM006()
    {
        var result = EventTypeMapper.ToListResponse();

        var toolEvent = result.Items.Single(e => e.Name == "ToolInvocationAccepted");
        toolEvent.Value.Should().Be(18);
        toolEvent.Category.Should().Be("ToolInvocation");
        toolEvent.Description.Should().Be("Request to invoke a tool was accepted and queued.");
    }

    [Fact(DisplayName = "ETM-007: ToListResponse should categorize lifecycle events correctly")]
    public void ETM007()
    {
        var result = EventTypeMapper.ToListResponse();

        var lifecycleEvents = result.Items.Where(e => e.Category == "Lifecycle").ToList();
        lifecycleEvents.Select(e => e.Name).Should().Contain(new[]
        {
            "Starting", "Started", "StartFailed",
            "Stopping", "Stopped", "StopFailed",
            "ServerCreated", "ServerDeleted"
        });
    }

    [Fact(DisplayName = "ETM-008: ToListResponse should categorize configuration events correctly")]
    public void ETM008()
    {
        var result = EventTypeMapper.ToListResponse();

        var configEvents = result.Items.Where(e => e.Category == "Configuration").ToList();
        configEvents.Select(e => e.Name).Should().BeEquivalentTo(new[]
        {
            "ConfigurationCreated", "ConfigurationUpdated", "ConfigurationDeleted"
        });
    }

    [Fact(DisplayName = "ETM-009: ToListResponse should categorize metadata events correctly")]
    public void ETM009()
    {
        var result = EventTypeMapper.ToListResponse();

        var metadataEvents = result.Items.Where(e => e.Category == "Metadata").ToList();
        metadataEvents.Select(e => e.Name).Should().BeEquivalentTo(new[]
        {
            "ToolsRetrieving", "ToolsRetrieved", "ToolsRetrievalFailed",
            "PromptsRetrieving", "PromptsRetrieved", "PromptsRetrievalFailed",
            "ResourcesRetrieving", "ResourcesRetrieved", "ResourcesRetrievalFailed"
        });
    }

    [Fact(DisplayName = "ETM-010: ToListResponse should categorize tool invocation events correctly")]
    public void ETM010()
    {
        var result = EventTypeMapper.ToListResponse();

        var toolInvocationEvents = result.Items.Where(e => e.Category == "ToolInvocation").ToList();
        toolInvocationEvents.Select(e => e.Name).Should().BeEquivalentTo(new[]
        {
            "ToolInvocationAccepted", "ToolInvoking", "ToolInvoked", "ToolInvocationFailed"
        });
    }

    [Fact(DisplayName = "ETM-011: ToListResponse should return same instance on multiple calls")]
    public void ETM011()
    {
        var result1 = EventTypeMapper.ToListResponse();
        var result2 = EventTypeMapper.ToListResponse();

        result1.Items.Should().BeSameAs(result2.Items);
    }

    [Fact(DisplayName = "ETM-012: ToListResponse should have unique values for all event types")]
    public void ETM012()
    {
        var result = EventTypeMapper.ToListResponse();

        var values = result.Items.Select(e => e.Value).ToList();
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "ETM-013: ToListResponse should have unique names for all event types")]
    public void ETM013()
    {
        var result = EventTypeMapper.ToListResponse();

        var names = result.Items.Select(e => e.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "ETM-014: All categories should be valid McpServerEventCategory values")]
    public void ETM014()
    {
        var result = EventTypeMapper.ToListResponse();

        var validCategories = Enum.GetNames<McpServerEventCategory>();
        foreach (var item in result.Items)
        {
            validCategories.Should().Contain(item.Category);
        }
    }
}
