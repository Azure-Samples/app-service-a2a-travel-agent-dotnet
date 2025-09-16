using Microsoft.AspNetCore.Mvc;
using TravelAgent.Core.Services;

namespace TravelAgent.Api.Controllers;

/// <summary>
/// Health check controller.
/// Provides health status endpoints for monitoring and Azure App Service.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IChatCompletionService _chatCompletionService;

    public HealthController(ILogger<HealthController> logger, IChatCompletionService chatCompletionService)
    {
        _logger = logger;
        _chatCompletionService = chatCompletionService;
    }

    /// <summary>
    /// Health check endpoint for Azure App Service.
    /// Equivalent to Python's health_check endpoint.
    /// </summary>
    [HttpGet]
    public ActionResult<object> GetHealth()
    {
        try
        {
            return Ok(new 
            { 
                status = "healthy", 
                service = "semantic-kernel-travel-agent-dotnet",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new 
            { 
                status = "unhealthy", 
                service = "semantic-kernel-travel-agent-dotnet",
                error = "Health check failed",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Detailed health check that includes AI service configuration status.
    /// </summary>
    [HttpGet("detailed")]
    public async Task<ActionResult<object>> GetDetailedHealth()
    {
        try
        {
            bool aiServiceConfigured = false;
            string aiServiceError = "";

            try
            {
                // Try to get the chat completion service to verify configuration
                await _chatCompletionService.GetChatCompletionServiceAsync();
                aiServiceConfigured = true;
            }
            catch
            {
                aiServiceError = "Configuration error";
            }

            return Ok(new 
            { 
                status = aiServiceConfigured ? "healthy" : "degraded",
                service = "semantic-kernel-travel-agent-dotnet",
                aiService = new
                {
                    configured = aiServiceConfigured,
                    error = aiServiceError
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            return StatusCode(500, new 
            { 
                status = "unhealthy", 
                service = "semantic-kernel-travel-agent-dotnet",
                error = "Health check failed",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
