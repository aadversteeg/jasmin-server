using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Resources;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.McpServers.Resources;

public class ResourceMapperTests
{
    [Fact(DisplayName = "RMP-001: ToResponse should map resource correctly")]
    public void RMP001()
    {
        var resource = new McpResource(
            "config",
            "file:///etc/config.json",
            "Configuration",
            "App config",
            "application/json");

        var result = ResourceMapper.ToResponse(resource);

        result.Name.Should().Be("config");
        result.Uri.Should().Be("file:///etc/config.json");
        result.Title.Should().Be("Configuration");
        result.Description.Should().Be("App config");
        result.MimeType.Should().Be("application/json");
    }

    [Fact(DisplayName = "RMP-002: ToResponse should handle null optional properties")]
    public void RMP002()
    {
        var resource = new McpResource("simple", "file:///test", null, null, null);

        var result = ResourceMapper.ToResponse(resource);

        result.Name.Should().Be("simple");
        result.Uri.Should().Be("file:///test");
        result.Title.Should().BeNull();
        result.Description.Should().BeNull();
        result.MimeType.Should().BeNull();
    }

    [Fact(DisplayName = "RMP-003: ToListResponse should map metadata with resources")]
    public void RMP003()
    {
        var resources = new List<McpResource>
        {
            new("resource1", "file:///r1", "Resource 1", null, null),
            new("resource2", "file:///r2", "Resource 2", null, null)
        };
        var metadata = new McpServerMetadata(
            null,
            null,
            resources,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);

        var result = ResourceMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("resource1");
        result.Items[1].Name.Should().Be("resource2");
        result.RetrievedAt.Should().StartWith("2024-01-15T10:30:00");
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "RMP-004: ToListResponse should return empty list for null metadata")]
    public void RMP004()
    {
        var result = ResourceMapper.ToListResponse(null, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.RetrievedAt.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "RMP-005: ToListResponse should return empty list when resources are null")]
    public void RMP005()
    {
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, null);

        var result = ResourceMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.RetrievedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "RMP-006: ToListResponse should include errors for Resources category")]
    public void RMP006()
    {
        var errors = new List<McpServerMetadataError>
        {
            new("Tools", "Failed to retrieve tools"),
            new("Resources", "Failed to retrieve resources")
        };
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, errors);

        var result = ResourceMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Errors.Should().HaveCount(1);
        result.Errors![0].Category.Should().Be("Resources");
        result.Errors[0].Message.Should().Be("Failed to retrieve resources");
    }

    [Fact(DisplayName = "RMP-007: ToListResponse should convert timestamp to specified timezone")]
    public void RMP007()
    {
        var metadata = new McpServerMetadata(
            null,
            null,
            new List<McpResource>(),
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = ResourceMapper.ToListResponse(metadata, amsterdamTz);

        result.RetrievedAt.Should().StartWith("2024-01-15T11:30:00");
        result.RetrievedAt.Should().EndWith("+01:00");
    }

    [Fact(DisplayName = "RMP-008: ToListResponse should not include errors from other categories")]
    public void RMP008()
    {
        var errors = new List<McpServerMetadataError>
        {
            new("Tools", "Tools error"),
            new("Prompts", "Prompts error")
        };
        var metadata = new McpServerMetadata(null, null, null, DateTime.UtcNow, errors);

        var result = ResourceMapper.ToListResponse(metadata, TimeZoneInfo.Utc);

        result.Errors.Should().BeNull();
    }
}
