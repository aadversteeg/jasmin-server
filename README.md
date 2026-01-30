# Jasmin

Jasmin is a .NET web API for managing MCP (Model Context Protocol) server configurations. It provides a RESTful interface to create, read, update, and delete MCP server definitions stored in a JSON configuration file.

## Features

- Full CRUD operations for MCP server configurations
- Thread-safe file access with concurrent read support
- Atomic file writes to prevent data corruption
- Layered architecture with separation of concerns

## Architecture

The solution follows a clean layered architecture:

- **Core.Domain** - Domain models, value objects, and error definitions
- **Core.Application** - Service interfaces and business logic
- **Core.Infrastructure.McpServers.FileStorage** - File-based repository implementation
- **Core.Infrastructure.WebApp** - ASP.NET Core Web API controllers and models

## API Endpoints

| Method | Route | Description | Response |
|--------|-------|-------------|----------|
| GET | `/v1/mcp-servers` | List all configured servers | 200 OK |
| GET | `/v1/mcp-servers/{id}` | Get server details by ID | 200 OK / 404 |
| POST | `/v1/mcp-servers` | Create a new server | 201 Created / 400 / 409 |
| PUT | `/v1/mcp-servers/{id}` | Update an existing server | 200 OK / 404 / 400 |
| DELETE | `/v1/mcp-servers/{id}` | Delete a server | 204 No Content / 404 |

### Request/Response Examples

**Create Server (POST)**
```json
{
  "name": "chronos",
  "command": "docker",
  "args": ["run", "--rm", "-i", "aadversteeg/chronos-mcp-server:latest"],
  "env": { "TZ": "Europe/Amsterdam" }
}
```

**Server Details Response**
```json
{
  "name": "chronos",
  "command": "docker",
  "args": ["run", "--rm", "-i", "aadversteeg/chronos-mcp-server:latest"],
  "env": { "TZ": "Europe/Amsterdam" }
}
```

## Configuration

MCP server configurations are stored in `~/.mcp-servers/config.json`. The file is created automatically when the first server is added.

## Build and Run

```bash
cd src
dotnet build
dotnet run --project Core.Infrastructure.WebApp
```

## Run Tests

```bash
cd src
dotnet test
```

## Requirements

- .NET 10.0 or later