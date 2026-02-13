using Core.Application.Requests;
using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory;

public class RequestHandlerRegistryTests
{
    [Fact(DisplayName = "RHR-001: GetHandler should return registered handler")]
    public void RHR001()
    {
        var registry = new RequestHandlerRegistry();
        var handlerMock = new Mock<IRequestHandler>();
        registry.Register(RequestActions.McpServer.Start, handlerMock.Object);

        var result = registry.GetHandler(RequestActions.McpServer.Start);

        result.Should().BeSameAs(handlerMock.Object);
    }

    [Fact(DisplayName = "RHR-002: GetHandler should return null for unregistered action")]
    public void RHR002()
    {
        var registry = new RequestHandlerRegistry();

        var result = registry.GetHandler(RequestActions.McpServer.Start);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "RHR-003: Register should overwrite existing handler for same action")]
    public void RHR003()
    {
        var registry = new RequestHandlerRegistry();
        var handler1 = new Mock<IRequestHandler>();
        var handler2 = new Mock<IRequestHandler>();

        registry.Register(RequestActions.McpServer.Start, handler1.Object);
        registry.Register(RequestActions.McpServer.Start, handler2.Object);

        var result = registry.GetHandler(RequestActions.McpServer.Start);
        result.Should().BeSameAs(handler2.Object);
    }

    [Fact(DisplayName = "RHR-004: GetHandler should differentiate between actions")]
    public void RHR004()
    {
        var registry = new RequestHandlerRegistry();
        var startHandler = new Mock<IRequestHandler>();
        var stopHandler = new Mock<IRequestHandler>();

        registry.Register(RequestActions.McpServer.Start, startHandler.Object);
        registry.Register(RequestActions.McpServer.Instance.Stop, stopHandler.Object);

        registry.GetHandler(RequestActions.McpServer.Start).Should().BeSameAs(startHandler.Object);
        registry.GetHandler(RequestActions.McpServer.Instance.Stop).Should().BeSameAs(stopHandler.Object);
        registry.GetHandler(RequestActions.McpServer.Instance.InvokeTool).Should().BeNull();
    }
}
