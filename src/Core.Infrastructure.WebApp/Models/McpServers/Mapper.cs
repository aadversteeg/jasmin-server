using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Mapper for converting between domain models and request/response models.
/// </summary>
public static class Mapper
{
    public static ListResponse ToListResponse(McpServerInfo source) =>
        new(source.Id.Value, source.Command);

    public static DetailsResponse ToDetailsResponse(McpServerDefinition source) =>
        new(source.Id.Value, source.Command, source.Args, source.Env);

    public static Result<McpServerDefinition, Error> ToDomain(CreateRequest request) =>
        McpServerId.Create(request.Name)
            .OnSuccessMap(id => new McpServerDefinition(
                id,
                request.Command,
                (request.Args ?? []).AsReadOnly(),
                (request.Env ?? new Dictionary<string, string>()).AsReadOnly()));

    public static Result<McpServerDefinition, Error> ToDomain(McpServerId id, UpdateRequest request) =>
        Result<McpServerDefinition, Error>.Success(new McpServerDefinition(
            id,
            request.Command,
            (request.Args ?? []).AsReadOnly(),
            (request.Env ?? new Dictionary<string, string>()).AsReadOnly()));
}
