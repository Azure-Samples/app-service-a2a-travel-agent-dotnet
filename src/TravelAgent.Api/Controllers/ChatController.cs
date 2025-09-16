using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.Json;
using TravelAgent.Api.Models;
using TravelAgent.Core.Agents;

namespace TravelAgent.Api.Controllers;

/// <summary>
/// Chat controller for travel agent interactions.
/// Equivalent to Python's chat.py router with FastAPI endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISemanticKernelTravelAgent _travelAgent;
    private readonly ILogger<ChatController> _logger;
    
    // In-memory session store (equivalent to Python's active_sessions Dict)
    // In production, use Redis or database
    private static readonly ConcurrentDictionary<string, string> ActiveSessions = new();

    public ChatController(ISemanticKernelTravelAgent travelAgent, ILogger<ChatController> logger)
    {
        _travelAgent = travelAgent;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the travel agent and get a response.
    /// Equivalent to Python's send_message endpoint.
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatMessage chatMessage)
    {
        try
        {
            // Generate session ID if not provided (equivalent to Python's session ID generation)
            var sessionId = chatMessage.SessionId ?? Guid.NewGuid().ToString();

            // Store session (equivalent to Python's active_sessions[session_id] = session_id)
            ActiveSessions.TryAdd(sessionId, sessionId);

            // Get response from agent (equivalent to Python's travel_agent.invoke)
            var response = await _travelAgent.InvokeAsync(chatMessage.Message, sessionId);

            return Ok(new ChatResponse
            {
                Response = response.Content,
                SessionId = sessionId,
                IsComplete = response.IsTaskComplete,
                RequiresInput = response.RequireUserInput
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Stream a response from the travel agent.
    /// Equivalent to Python's stream_message endpoint.
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatMessage chatMessage)
    {
        try
        {
            // Generate session ID if not provided
            var sessionId = chatMessage.SessionId ?? Guid.NewGuid().ToString();

            // Store session
            ActiveSessions.TryAdd(sessionId, sessionId);

            // Set response headers for streaming (equivalent to Python's StreamingResponse headers)
            Response.ContentType = "text/plain";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Headers"] = "*";

            // Stream responses (equivalent to Python's generate_response async generator)
            await foreach (var partial in _travelAgent.StreamAsync(chatMessage.Message, sessionId))
            {
                var responseData = new
                {
                    content = partial.Content,
                    session_id = sessionId,
                    is_complete = partial.IsTaskComplete,
                    requires_input = partial.RequireUserInput
                };

                // Format as SSE (Server-Sent Events) - equivalent to Python's yield f"data: {response_data}\n\n"
                var jsonData = JsonSerializer.Serialize(responseData);
                await Response.WriteAsync($"data: {jsonData}\n\n");
                await Response.Body.FlushAsync();

                if (partial.IsTaskComplete)
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up streaming");
            var errorData = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorData}\n\n");
        }
    }

    /// <summary>
    /// Get list of active chat sessions.
    /// Equivalent to Python's get_active_sessions endpoint.
    /// </summary>
    [HttpGet("sessions")]
    public ActionResult<object> GetActiveSessions()
    {
        try
        {
            var sessions = ActiveSessions.Keys.ToArray();
            return Ok(new { sessions, count = sessions.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a specific chat session.
    /// Equivalent to Python's delete session endpoint.
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public ActionResult DeleteSession(string sessionId)
    {
        try
        {
            if (ActiveSessions.TryRemove(sessionId, out _))
            {
                _logger.LogInformation("Deleted session {SessionId}", sessionId);
                return Ok(new { message = "Session deleted successfully", sessionId });
            }

            return NotFound(new { error = "Session not found", sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}