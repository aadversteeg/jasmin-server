using Core.Domain.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Requests;

public class RequestActionsTests
{
    [Fact(DisplayName = "RAS-001: McpServer.Start should be mcp-server.start")]
    public void RAS001()
    {
        RequestActions.McpServer.Start.Value.Should().Be("mcp-server.start");
    }

    [Fact(DisplayName = "RAS-002: McpServer.Instance.Stop should be mcp-server.instance.stop")]
    public void RAS002()
    {
        RequestActions.McpServer.Instance.Stop.Value.Should().Be("mcp-server.instance.stop");
    }

    [Fact(DisplayName = "RAS-003: McpServer.Instance.InvokeTool should be mcp-server.instance.invoke-tool")]
    public void RAS003()
    {
        RequestActions.McpServer.Instance.InvokeTool.Value.Should().Be("mcp-server.instance.invoke-tool");
    }

    [Fact(DisplayName = "RAS-004: McpServer.Instance.GetPrompt should be mcp-server.instance.get-prompt")]
    public void RAS004()
    {
        RequestActions.McpServer.Instance.GetPrompt.Value.Should().Be("mcp-server.instance.get-prompt");
    }

    [Fact(DisplayName = "RAS-005: McpServer.Instance.ReadResource should be mcp-server.instance.read-resource")]
    public void RAS005()
    {
        RequestActions.McpServer.Instance.ReadResource.Value.Should().Be("mcp-server.instance.read-resource");
    }

    [Fact(DisplayName = "RAS-006: McpServer.Instance.RefreshMetadata should be mcp-server.instance.refresh-metadata")]
    public void RAS006()
    {
        RequestActions.McpServer.Instance.RefreshMetadata.Value.Should().Be("mcp-server.instance.refresh-metadata");
    }

    [Fact(DisplayName = "RAS-007: Instance actions should be children of mcp-server")]
    public void RAS007()
    {
        var mcpServer = new RequestAction("mcp-server");

        RequestActions.McpServer.Start.IsChildOf(mcpServer).Should().BeTrue();
        RequestActions.McpServer.Instance.Stop.IsChildOf(mcpServer).Should().BeTrue();
        RequestActions.McpServer.Instance.InvokeTool.IsChildOf(mcpServer).Should().BeTrue();
    }

    [Fact(DisplayName = "RAS-008: Instance actions should be children of mcp-server.instance")]
    public void RAS008()
    {
        var instance = new RequestAction("mcp-server.instance");

        RequestActions.McpServer.Instance.Stop.IsChildOf(instance).Should().BeTrue();
        RequestActions.McpServer.Instance.InvokeTool.IsChildOf(instance).Should().BeTrue();
        RequestActions.McpServer.Instance.GetPrompt.IsChildOf(instance).Should().BeTrue();
        RequestActions.McpServer.Instance.ReadResource.IsChildOf(instance).Should().BeTrue();
        RequestActions.McpServer.Instance.RefreshMetadata.IsChildOf(instance).Should().BeTrue();
    }

    [Fact(DisplayName = "RAS-009: McpServer.Start should not be child of mcp-server.instance")]
    public void RAS009()
    {
        var instance = new RequestAction("mcp-server.instance");

        RequestActions.McpServer.Start.IsChildOf(instance).Should().BeFalse();
    }

    [Fact(DisplayName = "RAS-010: McpServer.TestConfiguration should be mcp-server.test-configuration")]
    public void RAS010()
    {
        RequestActions.McpServer.TestConfiguration.Value.Should().Be("mcp-server.test-configuration");
    }

    [Fact(DisplayName = "RAS-011: McpServer.TestConfiguration should be child of mcp-server")]
    public void RAS011()
    {
        var mcpServer = new RequestAction("mcp-server");

        RequestActions.McpServer.TestConfiguration.IsChildOf(mcpServer).Should().BeTrue();
    }
}
