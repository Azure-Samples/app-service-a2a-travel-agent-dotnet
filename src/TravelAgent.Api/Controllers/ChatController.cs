using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.Json;
using TravelAgent.Api.Models;
using TravelAgent.Core.Agents;

namespace TravelAgent.Api.Controllers;

/// <summary>
/// Chat controller for travel agent interactions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISemanticKernelTravelAgent _travelAgent;
    private readonly ILogger<ChatController> _logger;
    
    // In-memory session store - in production, use Redis or database
    private static readonly ConcurrentDictionary<string, DateTime> ActiveSessions = new();
    private static readonly Timer SessionCleanupTimer = new(CleanupExpiredSessions, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

    public ChatController(ISemanticKernelTravelAgent travelAgent, ILogger<ChatController> logger)
    {
        _travelAgent = travelAgent;
        _logger = logger;
    }

    private static void CleanupExpiredSessions(object? state)
    {
        var expiredSessions = ActiveSessions
            .Where(kvp => DateTime.UtcNow - kvp.Value > TimeSpan.FromHours(2))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            ActiveSessions.TryRemove(sessionId, out _);
        }
    }

    /// <summary>
    /// Send a message to the travel agent and get a response.
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatMessage chatMessage)
    {
        // Input validation
        if (chatMessage == null)
            return BadRequest(new { error = "Request body cannot be null" });
            
        if (string.IsNullOrWhiteSpace(chatMessage.Message))
            return BadRequest(new { error = "Message cannot be empty" });
            
        if (chatMessage.Message.Length > 4000)
            return BadRequest(new { error = "Message too long. Maximum 4000 characters allowed" });

        try
        {
            // Generate session ID if not provided
            var sessionId = chatMessage.SessionId ?? Guid.NewGuid().ToString();

            // Store session with timestamp
            ActiveSessions.TryAdd(sessionId, DateTime.UtcNow);

            // Get response from agent
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
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Stream a response from the travel agent.
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatMessage chatMessage)
    {
        // Input validation
        if (chatMessage == null || string.IsNullOrWhiteSpace(chatMessage.Message))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("data: {\"error\": \"Invalid message\"}\n\n");
            return;
        }
        
        if (chatMessage.Message.Length > 4000)
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("data: {\"error\": \"Message too long\"}\n\n");
            return;
        }

        try
        {
            // Generate session ID if not provided
            var sessionId = chatMessage.SessionId ?? Guid.NewGuid().ToString();

            // Store session with timestamp
            ActiveSessions.TryAdd(sessionId, DateTime.UtcNow);

            // Set response headers for streaming
            Response.ContentType = "text/plain";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Headers"] = "*";

            // Stream responses
            await foreach (var partial in _travelAgent.StreamAsync(chatMessage.Message, sessionId))
            {
                var responseData = new
                {
                    content = partial.Content,
                    session_id = sessionId,
                    is_complete = partial.IsTaskComplete,
                    requires_input = partial.RequireUserInput
                };

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
            var errorData = JsonSerializer.Serialize(new { error = "An error occurred while streaming the response" });
            await Response.WriteAsync($"data: {errorData}\n\n");
        }
    }

    /// <summary>
    /// Get list of active chat sessions.
    /// </summary>
    [HttpGet("sessions")]
    public ActionResult<object> GetActiveSessions()
    {
        try
        {
            var sessionCount = ActiveSessions.Count;
            return Ok(new { count = sessionCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return StatusCode(500, new { error = "Unable to retrieve sessions" });
        }
    }

    /// <summary>
    /// Delete a specific chat session.
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public ActionResult DeleteSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { error = "Session ID cannot be empty" });
            
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
            return StatusCode(500, new { error = "Unable to delete session" });
        }
    }
}