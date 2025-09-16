using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace TravelAgent.Core.Services;

/// <summary>
/// Implementation of chat completion services.
/// Handles Azure OpenAI and OpenAI service configuration.
/// Equivalent to Python's get_chat_completion_service functions.
/// </summary>
public class ChatCompletionService : IChatCompletionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatCompletionService> _logger;
    private readonly DefaultAzureCredential _azureCredential;

    public ChatCompletionService(
        IConfiguration configuration, 
        ILogger<ChatCompletionService> logger,
        DefaultAzureCredential azureCredential)
    {
        _configuration = configuration;
        _logger = logger;
        _azureCredential = azureCredential;
    }

    /// <summary>
    /// Get Azure OpenAI chat completion service with managed identity support.
    /// </summary>
    public async Task<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService> GetChatCompletionServiceAsync()
    {
        // Return Azure OpenAI service by default
        return await GetAzureOpenAIServiceAsync();
    }

    public Task<AzureOpenAIChatCompletionService> GetAzureOpenAIServiceAsync()
    {
        var endpoint = _configuration["Azure:OpenAI:Endpoint"] 
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deploymentName = _configuration["Azure:OpenAI:DeploymentName"] 
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");
        var apiVersion = _configuration["Azure:OpenAI:ApiVersion"] 
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION");
        var apiKey = _configuration["Azure:OpenAI:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        if (string.IsNullOrEmpty(endpoint))
            throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is required");
        if (string.IsNullOrEmpty(deploymentName))
            throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is required");
        if (string.IsNullOrEmpty(apiVersion))
            throw new InvalidOperationException("AZURE_OPENAI_API_VERSION is required");

        try
        {
            // Use managed identity if no API key is provided (equivalent to Python's managed identity flow)
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Using Azure managed identity for authentication");
                return Task.FromResult(new AzureOpenAIChatCompletionService(
                    deploymentName,
                    endpoint,
                    _azureCredential));
            }
            else
            {
                // Fallback to API key authentication for local development
                _logger.LogInformation("Using API key authentication for Azure OpenAI");
                return Task.FromResult(new AzureOpenAIChatCompletionService(
                    deploymentName: deploymentName,
                    endpoint: endpoint,
                    apiKey: apiKey));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Azure OpenAI service");
            throw;
        }
    }

    /// <summary>
    /// Get OpenAI chat completion service.
    /// Equivalent to Python's _get_openai_chat_completion_service.
    /// </summary>
    public Task<OpenAIChatCompletionService> GetOpenAIServiceAsync()
    {
        var apiKey = _configuration["OpenAI:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var modelId = _configuration["OpenAI:ModelId"] 
            ?? Environment.GetEnvironmentVariable("OPENAI_MODEL_ID")
            ?? "gpt-4";

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is required");

        try
        {
            _logger.LogInformation("Creating OpenAI service with model {ModelId}", modelId);
            return Task.FromResult(new OpenAIChatCompletionService(
                modelId: modelId,
                apiKey: apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create OpenAI service");
            throw;
        }
    }
}