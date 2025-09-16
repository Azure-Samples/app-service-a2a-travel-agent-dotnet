using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TravelAgent.Core.Models;

/// <summary>
/// A Response Format model to direct how the model should respond.
/// Equivalent to Python's ResponseFormat BaseModel.
/// </summary>
public class ResponseFormat
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResponseStatus Status { get; set; } = ResponseStatus.InputRequired;

    [Required]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response status enumeration.
/// Equivalent to Python's Literal['input_required', 'completed', 'error'].
/// </summary>
public enum ResponseStatus
{
    [JsonPropertyName("input_required")]
    InputRequired,
    
    [JsonPropertyName("completed")]
    Completed,
    
    [JsonPropertyName("error")]
    Error
}

/// <summary>
/// Agent response data structure for streaming responses.
/// Equivalent to Python's agent response format.
/// </summary>
public record AgentResponse
{
    public string Content { get; init; } = string.Empty;
    public string Type { get; init; } = "text";
    public bool IsPartial { get; init; } = false;
    public string? AgentName { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public bool IsTaskComplete { get; init; } = false;
    public bool RequireUserInput { get; init; } = false;
}