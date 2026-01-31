using Core.Application;
using Core.Infrastructure.McpServers.FileStorage;
using Core.Infrastructure.Messaging.InMemory;
using Core.Infrastructure.Messaging.SSE;
using Core.Infrastructure.ModelContextProtocol.Hosting;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Microsoft.OpenApi.Models;

namespace Core.Infrastructure.WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configFilePath = builder.Configuration["McpServerRepository:ConfigFilePath"]
            ?? "~/.mcp-servers/config.json";

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddMcpServerFileStorage(configFilePath);
        builder.Services.AddMcpServerHosting();
        builder.Services.AddInMemoryEventStore(builder.Configuration);
        builder.Services.AddSseEventStreaming();
        builder.Services.AddMcpServerConnectionStatusCaching(builder.Configuration);
        builder.Services.AddApplicationServices();

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Jasmin MCP Server API",
                Description = "API for retrieving MCP server configurations"
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors();
        app.UseHttpsRedirection();
        app.MapControllers();

        app.Run();
    }
}
