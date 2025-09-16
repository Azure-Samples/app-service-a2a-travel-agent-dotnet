using TravelAgent.Core.Models;

namespace TravelAgent.Core.Agents;

/// <summary>
/// Interface for Semantic Kernel Travel Agent.
/// Defines the contract for travel agent operations.
/// </summary>
public interface ISemanticKernelTravelAgent
{
    /// <summary>
    /// Handle synchronous tasks (equivalent to Python's invoke method).
    /// </summary>
    Task<AgentResponse> InvokeAsync(string userInput, string sessionId);

    /// <summary>
    /// Handle streaming tasks (equivalent to Python's stream method).
    /// </summary>
    IAsyncEnumerable<AgentResponse> StreamAsync(string userInput, string sessionId);
}