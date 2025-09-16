using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TravelAgent.Api.Models;

/// <summary>
/// Chat message model for API requests.
/// Equivalent to Python's ChatMessage BaseModel.
/// </summary>
public class ChatMessage
{
    [Required]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
}

/// <summary>
/// Chat response model for API responses.
/// Equivalent to Python's ChatResponse BaseModel.
/// </summary>
public class ChatResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("is_complete")]
    public bool IsComplete { get; set; }

    [JsonPropertyName("requires_input")]
    public bool RequiresInput { get; set; }
}