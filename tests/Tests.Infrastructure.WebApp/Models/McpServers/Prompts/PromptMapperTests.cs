using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Prompts;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.McpServers.Prompts;

public class PromptMapperTests
{
    [Fact(DisplayName = "PMP-001: ToResponse should map prompt correctly")]
    public void PMP001()
    {
        var arguments = new List<McpPromptArgument>
        {
            new("topic", "The topic", true),
            new("style", "Writing style", false)
        };
        var prompt = new McpPrompt("generate", "Generate Text", "Generates text", arguments);

        var result = PromptMapper.ToResponse(prompt);

        result.Name.Should().Be("generate");
        result.Title.Should().Be("Generate Text");
        result.Description.Should().Be("Generates text");
        result.Arguments.Should().HaveCount(2);
        result.Arguments![0].Name.Should().Be("topic");
        result.Arguments[0].Required.Should().BeTrue();
        result.Arguments[1].Name.Should().Be("style");
        result.Arguments[1].Required.Should().BeFalse();
    }

    [Fact(DisplayName = "PMP-002: ToResponse should handle null optional properties")]
    public void PMP002()
    {
        var prompt = new McpPrompt("simple", null, null, null);

        var result = PromptMapper.ToResponse(prompt);

        result.Name.Should().Be("simple");
        result.Title.Should().BeNull();
        result.Description.Should().BeNull();
        result.Arguments.Should().BeNull();
    }

    [Fact(DisplayName = "PMP-003: ToListResponse should map metadata with prompts")]
    public void PMP003()
    {
        var prompts = new List<McpPrompt>
        {
            new("prompt1", "Prompt 1", null, null),
            new("prompt2", "Prompt 2", null, null)
        };
        var metadata = new McpServerMetadata(
            null,
            prompts,
            null,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);

        var result = PromptMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("prompt1");
        result.Items[1].Name.Should().Be("prompt2");
        result.RetrievedAt.Should().StartWith("2024-01-15T10:30:00");
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "PMP-004: ToListResponse should return empty list for null metadata")]
    public void PMP004()
    {
        var result = PromptMapper.ToListResponse(null, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.RetrievedAt.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "PMP-005: ToListResponse should return empty list when prompts are null")]
    public void PMP005()
    {
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, null);

        var result = PromptMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.RetrievedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "PMP-006: ToListResponse should include errors for Prompts category")]
    public void PMP006()
    {
        var errors = new List<McpServerMetadataError>
        {
            new("Tools", "Failed to retrieve tools"),
            new("Prompts", "Failed to retrieve prompts")
        };
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, errors);

        var result = PromptMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Errors.Should().HaveCount(1);
        result.Errors![0].Category.Should().Be("Prompts");
        result.Errors[0].Message.Should().Be("Failed to retrieve prompts");
    }

    [Fact(DisplayName = "PMP-007: ToListResponse should convert timestamp to specified timezone")]
    public void PMP007()
    {
        var metadata = new McpServerMetadata(
            null,
            new List<McpPrompt>(),
            null,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = PromptMapper.ToListResponse(metadata, amsterdamTz);

        result.RetrievedAt.Should().StartWith("2024-01-15T11:30:00");
        result.RetrievedAt.Should().EndWith("+01:00");
    }

    [Fact(DisplayName = "PMP-008: ToResponse should map argument with null description")]
    public void PMP008()
    {
        var arguments = new List<McpPromptArgument> { new("param", null, false) };
        var prompt = new McpPrompt("test", null, null, arguments);

        var result = PromptMapper.ToResponse(prompt);

        result.Arguments.Should().HaveCount(1);
        result.Arguments![0].Description.Should().BeNull();
    }
}
