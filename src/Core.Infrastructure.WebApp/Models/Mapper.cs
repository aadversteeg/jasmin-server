using Core.Domain.McpServers;

namespace Core.Infrastructure.WebApp.Models;

/// <summary>
/// Mapper for converting domain models to response models.
/// </summary>
public static class Mapper
{
    public static McpServerInfoResponse ToResponse(McpServerInfo source) =>
        new(source.Id.Value, source.Command);

    public static McpServerDefinitionResponse ToResponse(McpServerDefinition source) =>
        new(source.Id.Value, source.Command, source.Args, source.Env);
}
