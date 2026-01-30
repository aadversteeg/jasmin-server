using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpResourceTests
{
    [Fact(DisplayName = "MCR-001: Resource should store all properties")]
    public void MCR001()
    {
        var resource = new McpResource(
            "config",
            "file:///etc/config.json",
            "Configuration File",
            "Application configuration",
            "application/json");

        resource.Name.Should().Be("config");
        resource.Uri.Should().Be("file:///etc/config.json");
        resource.Title.Should().Be("Configuration File");
        resource.Description.Should().Be("Application configuration");
        resource.MimeType.Should().Be("application/json");
    }

    [Fact(DisplayName = "MCR-002: Resource should allow null optional properties")]
    public void MCR002()
    {
        var resource = new McpResource("simple", "file:///test", null, null, null);

        resource.Name.Should().Be("simple");
        resource.Uri.Should().Be("file:///test");
        resource.Title.Should().BeNull();
        resource.Description.Should().BeNull();
        resource.MimeType.Should().BeNull();
    }

    [Fact(DisplayName = "MCR-003: Resources with same values should be equal")]
    public void MCR003()
    {
        var resource1 = new McpResource("test", "file:///test", "Test", null, "text/plain");
        var resource2 = new McpResource("test", "file:///test", "Test", null, "text/plain");

        resource1.Should().Be(resource2);
    }

    [Fact(DisplayName = "MCR-004: Resources with different URIs should not be equal")]
    public void MCR004()
    {
        var resource1 = new McpResource("test", "file:///test1", null, null, null);
        var resource2 = new McpResource("test", "file:///test2", null, null, null);

        resource1.Should().NotBe(resource2);
    }
}
