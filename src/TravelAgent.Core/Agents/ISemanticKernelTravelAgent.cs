using TravelAgent.Core.Models;

namespace TravelAgent.Core.Agents;

/// <summary>
/// Interface for Semantic Kernel Travel Agent.
/// </summary>
public interface ISemanticKernelTravelAgent : IAsyncDisposable
{
    /// <summary>
    /// Handle synchronous tasks.
    /// </summary>
    Task<Models.AgentResponse> InvokeAsync(string userInput, string sessionId = "default");

    /// <summary>
    /// Handle streaming tasks.
    /// </summary>
    IAsyncEnumerable<Models.AgentResponse> StreamAsync(string userInput, string sessionId = "default");
}