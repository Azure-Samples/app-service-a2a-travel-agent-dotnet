using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TravelAgent.Core.Models;
using TravelAgent.Core.Plugins;
using TravelAgent.Core.Services;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace TravelAgent.Core.Agents;

/// <summary>
/// Semantic Kernel Travel Agent implementation.
/// Wraps Semantic Kernel-based agents to handle travel-related tasks.
/// Equivalent to Python's SemanticKernelTravelAgent class.
/// </summary>
public class SemanticKernelTravelAgent : ISemanticKernelTravelAgent
{
    private readonly Services.IChatCompletionService _chatCompletionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticKernelTravelAgent> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;
    
    private ChatCompletionAgent? _agent;
    private AgentGroupChat? _groupChat;
    private readonly Dictionary<string, AgentGroupChat> _sessions = new();

    // Supported content types (equivalent to Python's SUPPORTED_CONTENT_TYPES)
    private static readonly string[] SupportedContentTypes = { "text", "text/plain" };

    public SemanticKernelTravelAgent(
        Services.IChatCompletionService chatCompletionService,
        IConfiguration configuration,
        ILogger<SemanticKernelTravelAgent> logger,
        ILoggerFactory loggerFactory,
        HttpClient httpClient)
    {
        _chatCompletionService = chatCompletionService;
        _configuration = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _httpClient = httpClient;
        
        // Don't initialize agents in constructor - do it lazily when needed
        // This prevents startup failures due to missing configuration
    }

    /// <summary>
    /// Initialize agents if not already initialized.
    /// </summary>
    private async Task EnsureAgentsInitializedAsync()
    {
        if (_agent != null) 
            return;

        _logger.LogInformation("Initializing Semantic Kernel Travel Agent");

        try
        {
            // Get the chat completion service
            var chatService = await _chatCompletionService.GetChatCompletionServiceAsync();

            // Create main travel agent kernel
            var mainBuilder = Kernel.CreateBuilder();
            mainBuilder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(chatService);
            var mainKernel = mainBuilder.Build();

            // Create currency kernel with plugin
            var currencyBuilder = Kernel.CreateBuilder();
            currencyBuilder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(chatService);
            var currencyKernel = currencyBuilder.Build();
            
            var currencyPlugin = new CurrencyPlugin(_httpClient, _loggerFactory.CreateLogger<CurrencyPlugin>());
            currencyKernel.ImportPluginFromObject(currencyPlugin, "CurrencyPlugin");

            // Create activity planner kernel
            var activityBuilder = Kernel.CreateBuilder();
            activityBuilder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(chatService);
            var activityKernel = activityBuilder.Build();

            // Currency Exchange Agent (equivalent to Python's CurrencyExchangeAgent)
            var currencyExchangeAgent = new ChatCompletionAgent
            {
                Instructions = 
                    "You specialize in handling currency-related requests from travelers. " +
                    "This includes providing current exchange rates, converting amounts between different currencies, " +
                    "explaining fees or charges related to currency exchange, and giving advice on the best practices for exchanging currency. " +
                    "Your goal is to assist travelers promptly and accurately with all currency-related questions.",
                Name = "CurrencyExchangeAgent",
                Kernel = currencyKernel,
            };

            // Activity Planner Agent (equivalent to Python's ActivityPlannerAgent) 
            var activityPlannerAgent = new ChatCompletionAgent
            {
                Instructions = 
                    "You specialize in planning and recommending activities for travelers. " +
                    "This includes suggesting sightseeing options, local events, dining recommendations, " +
                    "booking tickets for attractions, advising on travel itineraries, and ensuring activities " +
                    "align with traveler preferences and schedule. " +
                    "Your goal is to create enjoyable and personalized experiences for travelers.",
                Name = "ActivityPlannerAgent",
                Kernel = activityKernel,
            };

            // Main Travel Manager Agent (equivalent to Python's TravelManagerAgent)
            _agent = new ChatCompletionAgent
            {
                Instructions = 
                    "Your role is to carefully analyze the traveler's request and coordinate with specialized agents. " +
                    "Forward currency-related requests to the CurrencyExchangeAgent. " +
                    "Forward activity planning requests to the ActivityPlannerAgent. " +
                    "You can handle general travel queries directly. " +
                    "Always provide helpful, accurate, and personalized responses.",
                Name = "TravelManagerAgent",
                Kernel = mainKernel,
            };

            // Create group chat for multi-agent coordination
            _groupChat = new AgentGroupChat(currencyExchangeAgent, activityPlannerAgent, _agent);
            
            _logger.LogInformation("Travel agents initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Semantic Kernel Travel Agent");
            throw;
        }
    }

    /// <summary>
    /// Handle synchronous tasks (equivalent to Python's invoke method).
    /// </summary>
    public async Task<AgentResponse> InvokeAsync(string userInput, string sessionId = "default")
    {
        _logger.LogInformation("Processing sync request: {Input}", userInput);
        
        try
        {
            await EnsureAgentsInitializedAsync();

            // Use the main agent to get a response
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(userInput);
            
            var response = await _agent!.Kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>()
                .GetChatMessageContentAsync(chatHistory);
            
            return GetAgentResponse(response.Content ?? "I apologize, but I couldn't generate a response. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sync request");
            return new AgentResponse
            {
                Content = "I apologize, but I encountered an error processing your request. Please try again.",
                IsTaskComplete = true,
                RequireUserInput = false,
                Type = "error",
                IsPartial = false,
                AgentName = "TravelAssistant"
            };
        }
    }

    /// <summary>
    /// Handle streaming tasks (equivalent to Python's stream method).
    /// </summary>
    public IAsyncEnumerable<AgentResponse> StreamAsync(string userInput, string sessionId = "default")
    {
        return ProcessRequestAsync(userInput, sessionId);
    }



    /// <summary>
    /// Handle streaming tasks (equivalent to Python's stream method).
    /// </summary>
    public async IAsyncEnumerable<AgentResponse> ProcessRequestAsync(
        string userInput, 
        string sessionId = "default",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing user request: {Input}", userInput);
        
        AgentResponse? errorResponse = null;
        var responses = new List<AgentResponse>();
        
        try
        {
            await EnsureAgentsInitializedAsync();

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(userInput);
            var chunks = new List<string>();

            await foreach (var streamingContent in _agent!.Kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>()
                .GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(streamingContent.Content))
                {
                    chunks.Add(streamingContent.Content);
                }
            }

            // Combine all chunks into final response
            if (chunks.Count > 0)
            {
                var finalContent = string.Join("", chunks);
                responses.Add(GetAgentResponse(finalContent));
            }
            else
            {
                responses.Add(new AgentResponse
                {
                    Content = "I apologize, but I couldn't generate a response. Please try again.",
                    IsTaskComplete = true,
                    RequireUserInput = false,
                    Type = "response",
                    IsPartial = false,
                    AgentName = "TravelManagerAgent"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user request: {Message}", ex.Message);
            errorResponse = new AgentResponse
            {
                Content = "I apologize, but I encountered an error processing your request. Please try again.",
                IsTaskComplete = true,
                RequireUserInput = false,
                Type = "error",
                IsPartial = false,
                AgentName = "TravelManagerAgent"
            };
        }

        // Yield all responses
        foreach (var response in responses)
        {
            yield return response;
        }

        // Yield error response if there was one
        if (errorResponse != null)
        {
            yield return errorResponse;
        }
    }

    /// <summary>
    /// Extract structured response from agent's message content.
    /// Equivalent to Python's _get_agent_response method.
    /// </summary>
    private AgentResponse GetAgentResponse(string content)
    {
        // For now, return a simple response structure
        // Later we can add structured JSON parsing like the Python version
        return new AgentResponse
        {
            Content = content,
            IsTaskComplete = true,
            RequireUserInput = false,
            Type = "response",
            IsPartial = false,
            AgentName = "TravelManagerAgent"
        };
    }

    /// <summary>
    /// Ensure session exists for the given session ID.
    /// Equivalent to Python's _ensure_thread_exists method.
    /// </summary>
    private async Task EnsureSessionExistsAsync(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            if (_agent != null)
            {
                // Create new group chat session (equivalent to Python's thread creation)
                _sessions[sessionId] = new AgentGroupChat(_agent);
                _logger.LogInformation("Created new session {SessionId}", sessionId);
            }
        }

        await Task.CompletedTask; // Placeholder for any async session initialization
    }
}