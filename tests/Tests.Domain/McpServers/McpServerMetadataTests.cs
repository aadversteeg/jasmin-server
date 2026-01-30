using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerMetadataTests
{
    [Fact(DisplayName = "MSM-001: Metadata should store all properties")]
    public void MSM001()
    {
        var tools = new List<McpTool> { new("tool1", "Tool 1", null, null) };
        var prompts = new List<McpPrompt> { new("prompt1", "Prompt 1", null, null) };
        var resources = new List<McpResource> { new("resource1", "file:///test", null, null, null) };
        var timestamp = DateTime.UtcNow;

        var metadata = new McpServerMetadata(tools, prompts, resources, timestamp, null);

        metadata.Tools.Should().HaveCount(1);
        metadata.Prompts.Should().HaveCount(1);
        metadata.Resources.Should().HaveCount(1);
        metadata.RetrievedAtUtc.Should().Be(timestamp);
        metadata.RetrievalErrors.Should().BeNull();
    }

    [Fact(DisplayName = "MSM-002: Metadata should allow null collections")]
    public void MSM002()
    {
        var timestamp = DateTime.UtcNow;

        var metadata = new McpServerMetadata(null, null, null, timestamp, null);

        metadata.Tools.Should().BeNull();
        metadata.Prompts.Should().BeNull();
        metadata.Resources.Should().BeNull();
    }

    [Fact(DisplayName = "MSM-003: Metadata should store retrieval errors")]
    public void MSM003()
    {
        var errors = new List<McpServerMetadataError>
        {
            new("Tools", "Failed to retrieve tools"),
            new("Prompts", "Prompts not supported")
        };
        var timestamp = DateTime.UtcNow;

        var metadata = new McpServerMetadata(null, null, null, timestamp, errors);

        metadata.RetrievalErrors.Should().HaveCount(2);
        metadata.RetrievalErrors![0].Category.Should().Be("Tools");
        metadata.RetrievalErrors[0].ErrorMessage.Should().Be("Failed to retrieve tools");
        metadata.RetrievalErrors[1].Category.Should().Be("Prompts");
    }

    [Fact(DisplayName = "MSM-004: Metadata with partial success should have both data and errors")]
    public void MSM004()
    {
        var tools = new List<McpTool> { new("tool1", null, null, null) };
        var errors = new List<McpServerMetadataError> { new("Prompts", "Error") };
        var timestamp = DateTime.UtcNow;

        var metadata = new McpServerMetadata(tools, null, null, timestamp, errors);

        metadata.Tools.Should().HaveCount(1);
        metadata.Prompts.Should().BeNull();
        metadata.RetrievalErrors.Should().HaveCount(1);
    }

    [Fact(DisplayName = "MSM-005: MetadataError should store category and message")]
    public void MSM005()
    {
        var error = new McpServerMetadataError("Resources", "Connection timeout");

        error.Category.Should().Be("Resources");
        error.ErrorMessage.Should().Be("Connection timeout");
    }

    [Fact(DisplayName = "MSM-006: MetadataErrors with same values should be equal")]
    public void MSM006()
    {
        var error1 = new McpServerMetadataError("Tools", "Error message");
        var error2 = new McpServerMetadataError("Tools", "Error message");

        error1.Should().Be(error2);
    }
}
