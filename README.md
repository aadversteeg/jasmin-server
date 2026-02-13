# Jasmin

Jasmin is a .NET web API for managing MCP (Model Context Protocol) server processes. It provides a RESTful interface to configure, start, and interact with MCP servers, with real-time event streaming via Server-Sent Events (SSE) and async request processing for tool invocations, prompt retrieval, and resource reading.

## Features

- Full CRUD operations for MCP server configurations
- Instance lifecycle management (start, stop, metadata retrieval)
- Async request processing with status tracking
- Real-time event streaming via SSE with reconnection support
- Instance stderr log capture and streaming
- Tool invocation, prompt retrieval, and resource reading
- Thread-safe file access with atomic writes
- Timezone-aware timestamp formatting
- Swagger/OpenAPI documentation

## Architecture

The solution follows a clean layered architecture:

- **Core.Domain** - Domain models, value objects, error definitions, request/event types
- **Core.Application** - Service interfaces, business logic, event factory
- **Core.Infrastructure.McpServers.FileStorage** - File-based configuration persistence
- **Core.Infrastructure.Messaging** - Messaging abstractions
- **Core.Infrastructure.Messaging.InMemory** - In-memory event store
- **Core.Infrastructure.Messaging.SSE** - Server-Sent Events streaming
- **Core.Infrastructure.ModelContextProtocol.Hosting** - MCP instance management and request handlers
- **Core.Infrastructure.ModelContextProtocol.InMemory** - In-memory request queue, store, and processor
- **Core.Infrastructure.WebApp** - ASP.NET Core Web API controllers and models

## API Endpoints

### MCP Servers (`/v1/mcp-servers`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/v1/mcp-servers` | List all configured servers |
| POST | `/v1/mcp-servers` | Create a new server |
| GET | `/v1/mcp-servers/{id}` | Get server details (`?include=configuration,instances,tools,prompts,resources,all`) |
| DELETE | `/v1/mcp-servers/{id}` | Delete a server |
| GET | `/v1/mcp-servers/{id}/configuration` | Get server configuration |
| PUT | `/v1/mcp-servers/{id}/configuration` | Update server configuration |
| DELETE | `/v1/mcp-servers/{id}/configuration` | Delete server configuration |
| POST | `/v1/mcp-servers/test-configuration` | Test a configuration without persisting |
| GET | `/v1/mcp-servers/{id}/instances` | List running instances |
| GET | `/v1/mcp-servers/{id}/instances/{iid}` | Get instance details (`?include=tools,prompts,resources,all`) |
| GET | `/v1/mcp-servers/{id}/instances/{iid}/tools` | Get instance tools |
| GET | `/v1/mcp-servers/{id}/instances/{iid}/prompts` | Get instance prompts |
| GET | `/v1/mcp-servers/{id}/instances/{iid}/resources` | Get instance resources |
| GET | `/v1/mcp-servers/{id}/instances/{iid}/logs` | Get instance stderr logs (`?afterLine=0&limit=100`) |
| GET | `/v1/mcp-servers/{id}/instances/{iid}/logs/stream` | Stream instance logs via SSE |
| GET | `/v1/mcp-servers/{id}/tools` | Get server tools |
| GET | `/v1/mcp-servers/{id}/prompts` | Get server prompts |
| GET | `/v1/mcp-servers/{id}/resources` | Get server resources |

### Requests (`/v1/requests`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/v1/requests` | Create an async request (returns 202) |
| GET | `/v1/requests` | List requests (`?target=&action=&status=&orderBy=createdAt&orderDirection=desc&from=&to=`) |
| GET | `/v1/requests/{id}` | Get request by ID |

Available actions: `mcp-server.start`, `mcp-server.instance.stop`, `mcp-server.instance.invoke-tool`, `mcp-server.instance.get-prompt`, `mcp-server.instance.read-resource`, `mcp-server.instance.refresh-metadata`.

### Events (`/v1/events`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/v1/events` | Get events (`?target=&eventType=&requestId=&orderDirection=desc&from=&to=`) |
| GET | `/v1/events/stream` | Stream events via SSE (`?target=`, supports `Last-Event-ID` header) |
| GET | `/v1/events/types` | Get all available event types |

## Request/Response Examples

### Create Server

**POST** `/v1/mcp-servers`
```json
{
  "name": "chronos",
  "configuration": {
    "command": "docker",
    "args": ["run", "--rm", "-i", "aadversteeg/chronos-mcp-server:latest"],
    "env": { "TZ": "Europe/Amsterdam" }
  }
}
```

### Create Request (Invoke Tool)

**POST** `/v1/requests`
```json
{
  "action": "mcp-server.instance.invoke-tool",
  "target": "mcp-servers/chronos/instances/abc-123",
  "parameters": {
    "toolName": "get_timezone",
    "input": { "tz": "America/New_York" }
  }
}
```

**Response** (202 Accepted):
```json
{
  "id": "d4f8a...",
  "action": "mcp-server.instance.invoke-tool",
  "target": "mcp-servers/chronos/instances/abc-123",
  "status": "pending",
  "createdAt": "2025-01-15T10:30:00+01:00",
  "parameters": {
    "toolName": "get_timezone",
    "input": { "tz": "America/New_York" }
  }
}
```

### Event Response

```json
{
  "eventType": "mcp-server.instance.started",
  "target": "mcp-servers/chronos/instances/abc-123",
  "timestamp": "2025-01-15T10:30:00+01:00",
  "payload": {
    "configuration": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "aadversteeg/chronos-mcp-server:latest"],
      "env": { "TZ": "Europe/Amsterdam" }
    }
  },
  "requestId": "d4f8a..."
}
```

## SSE Streaming

### Event Stream

Connect to `/v1/events/stream` to receive real-time events. Each SSE message uses the event type as the `event:` field:

```text
id: 2025-01-15T10:30:00.0000000Z
event: mcp-server.instance.started
data: {"eventType":"mcp-server.instance.started","target":"mcp-servers/chronos/instances/abc-123",...}
```

Supports reconnection via the `Last-Event-ID` header or `?lastEventId=` query parameter to replay missed events.

### Log Stream

Connect to `/v1/mcp-servers/{id}/instances/{iid}/logs/stream` to stream instance stderr output:

```text
id: 42
event: instance-log
data: {"lineNumber":42,"timestamp":"2025-01-15T10:30:00+01:00","message":"Server listening on port 3000"}
```

## Configuration

MCP server configurations are stored in `~/.mcp-servers/config.json` (configurable via `appsettings.json`).

```json
{
  "McpServerRepository": {
    "ConfigFilePath": "~/.mcp-servers/config.json"
  },
  "McpServerStatus": {
    "DefaultTimeZone": "Europe/Amsterdam"
  },
  "McpServerHosting": {
    "ToolInvocationTimeoutSeconds": 120,
    "ConnectionTimeoutSeconds": 30
  }
}
```

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
