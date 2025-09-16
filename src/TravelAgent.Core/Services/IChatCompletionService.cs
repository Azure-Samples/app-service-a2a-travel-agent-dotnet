using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Azure.AI.OpenAI;

namespace TravelAgent.Core.Services;

public interface IChatCompletionService
{
    /// <summary>
    /// Gets or creates a chat completion service based on environment configuration
    /// </summary>
    Task<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService> GetChatCompletionServiceAsync();
    
    /// <summary>
    /// Gets the Azure OpenAI chat completion service
    /// </summary>
    Task<AzureOpenAIChatCompletionService> GetAzureOpenAIServiceAsync();
    
    /// <summary>
    /// Gets the OpenAI chat completion service
    /// </summary>
    Task<OpenAIChatCompletionService> GetOpenAIServiceAsync();
}