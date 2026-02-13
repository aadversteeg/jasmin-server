using Core.Application.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Application.Requests;

public class TargetUriTests
{
    [Fact(DisplayName = "TUR-001: McpServer should build correct URI")]
    public void TUR001()
    {
        var target = TargetUri.McpServer("my-server");

        target.Should().Be("mcp-servers/my-server");
    }

    [Fact(DisplayName = "TUR-002: McpServerInstance should build correct URI")]
    public void TUR002()
    {
        var target = TargetUri.McpServerInstance("my-server", "abc-123");

        target.Should().Be("mcp-servers/my-server/instances/abc-123");
    }

    [Fact(DisplayName = "TUR-003: TryParseMcpServer should parse valid server target")]
    public void TUR003()
    {
        var result = TargetUri.TryParseMcpServer("mcp-servers/my-server", out var name);

        result.Should().BeTrue();
        name.Should().Be("my-server");
    }

    [Fact(DisplayName = "TUR-004: TryParseMcpServer should reject instance target")]
    public void TUR004()
    {
        var result = TargetUri.TryParseMcpServer("mcp-servers/my-server/instances/abc", out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-005: TryParseMcpServer should reject empty string")]
    public void TUR005()
    {
        var result = TargetUri.TryParseMcpServer("", out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-006: TryParseMcpServer should reject null")]
    public void TUR006()
    {
        var result = TargetUri.TryParseMcpServer(null!, out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-007: TryParseMcpServer should reject wrong prefix")]
    public void TUR007()
    {
        var result = TargetUri.TryParseMcpServer("other/my-server", out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-008: TryParseMcpServer should reject missing name")]
    public void TUR008()
    {
        var result = TargetUri.TryParseMcpServer("mcp-servers/", out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-009: TryParseMcpServerInstance should parse valid instance target")]
    public void TUR009()
    {
        var result = TargetUri.TryParseMcpServerInstance("mcp-servers/my-server/instances/abc-123", out var name, out var instanceId);

        result.Should().BeTrue();
        name.Should().Be("my-server");
        instanceId.Should().Be("abc-123");
    }

    [Fact(DisplayName = "TUR-010: TryParseMcpServerInstance should reject server-only target")]
    public void TUR010()
    {
        var result = TargetUri.TryParseMcpServerInstance("mcp-servers/my-server", out _, out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-011: TryParseMcpServerInstance should reject empty string")]
    public void TUR011()
    {
        var result = TargetUri.TryParseMcpServerInstance("", out _, out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-012: TryParseMcpServerInstance should reject null")]
    public void TUR012()
    {
        var result = TargetUri.TryParseMcpServerInstance(null!, out _, out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-013: TryParseMcpServerInstance should reject wrong middle segment")]
    public void TUR013()
    {
        var result = TargetUri.TryParseMcpServerInstance("mcp-servers/my-server/other/abc", out _, out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-014: TryParseMcpServerInstance should reject missing instance ID")]
    public void TUR014()
    {
        var result = TargetUri.TryParseMcpServerInstance("mcp-servers/my-server/instances/", out _, out _);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "TUR-015: McpServer build and parse roundtrip should succeed")]
    public void TUR015()
    {
        var target = TargetUri.McpServer("test-server");

        var result = TargetUri.TryParseMcpServer(target, out var name);

        result.Should().BeTrue();
        name.Should().Be("test-server");
    }

    [Fact(DisplayName = "TUR-016: McpServerInstance build and parse roundtrip should succeed")]
    public void TUR016()
    {
        var target = TargetUri.McpServerInstance("test-server", "inst-456");

        var result = TargetUri.TryParseMcpServerInstance(target, out var name, out var instanceId);

        result.Should().BeTrue();
        name.Should().Be("test-server");
        instanceId.Should().Be("inst-456");
    }
}
