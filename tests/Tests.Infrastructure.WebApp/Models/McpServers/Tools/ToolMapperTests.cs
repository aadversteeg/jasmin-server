using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Tools;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.McpServers.Tools;

public class ToolMapperTests
{
    [Fact(DisplayName = "TMP-001: ToResponse should map tool correctly")]
    public void TMP001()
    {
        var tool = new McpTool("read_file", "Read File", "Reads a file", "{\"type\":\"object\"}");

        var result = ToolMapper.ToResponse(tool);

        result.Name.Should().Be("read_file");
        result.Title.Should().Be("Read File");
        result.Description.Should().Be("Reads a file");
        result.InputSchema.Should().NotBeNull();
    }

    [Fact(DisplayName = "TMP-002: ToResponse should handle null optional properties")]
    public void TMP002()
    {
        var tool = new McpTool("simple_tool", null, null, null);

        var result = ToolMapper.ToResponse(tool);

        result.Name.Should().Be("simple_tool");
        result.Title.Should().BeNull();
        result.Description.Should().BeNull();
        result.InputSchema.Should().BeNull();
    }

    [Fact(DisplayName = "TMP-003: ToListResponse should map metadata with tools")]
    public void TMP003()
    {
        var tools = new List<McpTool>
        {
            new("tool1", "Tool 1", null, null),
            new("tool2", "Tool 2", null, null)
        };
        var metadata = new McpServerMetadata(
            tools,
            null,
            null,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);

        var result = ToolMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("tool1");
        result.Items[1].Name.Should().Be("tool2");
        result.RetrievedAt.Should().StartWith("2024-01-15T10:30:00");
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "TMP-004: ToListResponse should return empty list for null metadata")]
    public void TMP004()
    {
        var result = ToolMapper.ToListResponse(null, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.RetrievedAt.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "TMP-005: ToListResponse should return empty list when tools are null")]
    public void TMP005()
    {
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, null);

        var result = ToolMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.RetrievedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "TMP-006: ToListResponse should include errors for Tools category")]
    public void TMP006()
    {
        var errors = new List<McpServerMetadataError>
        {
            new("Tools", "Failed to retrieve tools"),
            new("Prompts", "Failed to retrieve prompts")
        };
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, errors);

        var result = ToolMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Errors.Should().HaveCount(1);
        result.Errors![0].Category.Should().Be("Tools");
        result.Errors[0].Message.Should().Be("Failed to retrieve tools");
    }

    [Fact(DisplayName = "TMP-007: ToListResponse should convert timestamp to specified timezone")]
    public void TMP007()
    {
        var metadata = new McpServerMetadata(
            new List<McpTool>(),
            null,
            null,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = ToolMapper.ToListResponse(metadata, amsterdamTz);

        result.RetrievedAt.Should().StartWith("2024-01-15T11:30:00");
        result.RetrievedAt.Should().EndWith("+01:00");
    }

    [Fact(DisplayName = "TMP-008: ToResponse should deserialize InputSchema as object")]
    public void TMP008()
    {
        var tool = new McpTool("test", null, null, "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}}}");

        var result = ToolMapper.ToResponse(tool);

        result.InputSchema.Should().NotBeNull();
    }
}
