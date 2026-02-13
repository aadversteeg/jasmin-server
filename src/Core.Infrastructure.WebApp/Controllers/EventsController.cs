using System.Globalization;
using System.Text.Json;
using Core.Application.Events;
using Core.Domain.Events;
using Core.Domain.Paging;
using Core.Infrastructure.Messaging.SSE;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Domain.Models;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.Events;
using Core.Infrastructure.WebApp.Models.McpServers;
using Core.Infrastructure.WebApp.Models.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Error = Ave.Extensions.ErrorPaths.Error;

namespace Core.Infrastructure.WebApp.Controllers;

/// <summary>
/// Controller for global events endpoint.
/// </summary>
[ApiController]
[Route("v1/events")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly SseClientManager _sseClientManager;
    private readonly McpServerStatusOptions _statusOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventsController(
        IEventStore eventStore,
        SseClientManager sseClientManager,
        IOptions<McpServerStatusOptions> statusOptions,
        IOptions<JsonOptions> jsonOptions)
    {
        _eventStore = eventStore;
        _sseClientManager = sseClientManager;
        _statusOptions = statusOptions.Value;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    /// <summary>
    /// Gets events with optional paging, filtering, and sorting.
    /// </summary>
    /// <param name="target">Optional filter by target URI prefix (e.g. 'mcp-servers/my-server').</param>
    /// <param name="eventType">Optional filter by event type (e.g. 'mcp-server.instance.started').</param>
    /// <param name="requestId">Optional filter by request ID.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Number of items per page (1-100). Default: 20.</param>
    /// <param name="orderDirection">Sort direction: 'asc' or 'desc'. Default: 'desc'.</param>
    /// <param name="from">Filter events from this timestamp (inclusive).</param>
    /// <param name="to">Filter events up to this timestamp (inclusive).</param>
    /// <returns>A paged list of events.</returns>
    [HttpGet(Name = "GetEvents")]
    [ProducesResponseType(typeof(PagedResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetEvents(
        [FromQuery] string? target = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? requestId = null,
        [FromQuery] string? timeZone = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string orderDirection = "desc",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        var pagingResult = PagingParameters.Create(page, pageSize);
        if (pagingResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(pagingResult.Error));
        }

        var targetFilter = string.IsNullOrEmpty(target) ? null : target;

        EventType? eventTypeFilter = null;
        if (!string.IsNullOrEmpty(eventType))
        {
            eventTypeFilter = new EventType(eventType);
        }

        var dateFilter = new DateRangeFilter(from, to);
        var sortDir = orderDirection.ToLowerInvariant() == "asc"
            ? SortDirection.Ascending
            : SortDirection.Descending;

        var pagedEvents = _eventStore.GetEvents(
            pagingResult.Value,
            targetFilter,
            eventTypeFilter,
            requestId,
            dateFilter,
            sortDir);

        var response = Mapper.ToPagedResponse(pagedEvents, resolvedTimeZone);
        return Ok(response);
    }

    /// <summary>
    /// Streams events using Server-Sent Events (SSE).
    /// Supports reconnection via Last-Event-ID header or lastEventId query parameter to replay missed events.
    /// </summary>
    /// <param name="target">Optional filter by target URI prefix (e.g. 'mcp-servers/my-server').</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <param name="lastEventId">Optional last event ID for reconnection. If provided, events after this ID will be replayed. The Last-Event-ID header takes precedence if both are provided.</param>
    /// <param name="cancellationToken">Cancellation token for the stream.</param>
    /// <returns>A stream of events in SSE format.</returns>
    [HttpGet("stream", Name = "StreamEvents")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task StreamEvents(
        [FromQuery] string? target = null,
        [FromQuery] string? timeZone = null,
        [FromQuery] string? lastEventId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Check for Last-Event-ID header first, then query parameter for reconnection
        var lastEventIdValue = Request.Headers["Last-Event-ID"].FirstOrDefault() ?? lastEventId;
        DateTime? lastEventTimestamp = null;
        if (!string.IsNullOrEmpty(lastEventIdValue) &&
            DateTime.TryParse(lastEventIdValue, null, DateTimeStyles.RoundtripKind, out var parsed))
        {
            lastEventTimestamp = parsed.ToUniversalTime();
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        // Send initial comment to establish connection and trigger EventSource onopen
        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        var clientId = _sseClientManager.RegisterClient();
        try
        {
            // Replay missed events if reconnecting with Last-Event-ID
            if (lastEventTimestamp.HasValue)
            {
                var missedEvents = _eventStore.GetEventsAfter(
                    lastEventTimestamp.Value,
                    target);

                foreach (var @event in missedEvents)
                {
                    await SendSseEventAsync(@event, resolvedTimeZone, cancellationToken);
                }
            }

            // Stream live events
            await foreach (var @event in _sseClientManager.GetEventsAsync(clientId, cancellationToken))
            {
                if (target != null && !MatchesTargetPrefix(@event.Target, target))
                {
                    continue;
                }

                await SendSseEventAsync(@event, resolvedTimeZone, cancellationToken);
            }
        }
        finally
        {
            _sseClientManager.UnregisterClient(clientId);
        }
    }

    private async Task SendSseEventAsync(
        Event @event,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var eventResponse = Mapper.ToEventResponse(@event, timeZone);
        var json = JsonSerializer.Serialize(eventResponse, _jsonOptions);
        var eventId = @event.TimestampUtc.ToString("o");

        await Response.WriteAsync($"id: {eventId}\n", cancellationToken);
        await Response.WriteAsync($"event: {@event.Type.Value}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all available event types with their categories and descriptions.
    /// </summary>
    /// <returns>A list of all event types.</returns>
    [HttpGet("types", Name = "GetEventTypes")]
    [ProducesResponseType(typeof(EventTypeListResponse), StatusCodes.Status200OK)]
    public IActionResult GetEventTypes()
    {
        return Ok(EventTypeMapper.ToListResponse());
    }

    private TimeZoneInfo? ResolveTimeZone(string? requestedTimeZone)
    {
        var timeZoneId = requestedTimeZone ?? _statusOptions.DefaultTimeZone;

        if (string.IsNullOrEmpty(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }

    private static bool MatchesTargetPrefix(string eventTarget, string filter)
    {
        return eventTarget.Equals(filter, StringComparison.OrdinalIgnoreCase)
            || eventTarget.StartsWith(filter + "/", StringComparison.OrdinalIgnoreCase);
    }
}
