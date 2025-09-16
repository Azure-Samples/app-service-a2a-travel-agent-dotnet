using TravelAgent.Core.Agents;
using TravelAgent.Core.Services;
using Microsoft.SemanticKernel;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs for Azure App Service (handle PORT environment variable)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add HttpClient for external API calls (equivalent to httpx in Python)
builder.Services.AddHttpClient();

// Add Semantic Kernel services
builder.Services.AddScoped<TravelAgent.Core.Services.IChatCompletionService, ChatCompletionService>();
builder.Services.AddScoped<ISemanticKernelTravelAgent, SemanticKernelTravelAgent>();

// Configure Azure Identity (equivalent to DefaultAzureCredential in Python)
builder.Services.AddSingleton<DefaultAzureCredential>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure default files to serve index.html at root (must come before UseStaticFiles)
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

// Enable static file serving (equivalent to FastAPI StaticFiles mount)
app.UseStaticFiles();

// Enable routing
app.UseRouting();

// Enable CORS
app.UseCors("AllowAllOrigins");

// Map controllers (equivalent to FastAPI router includes)
app.MapControllers();

// A2A Agent Card endpoint (equivalent to Python get_agent_card)
app.MapGet("/agent-card", () =>
{
    // Return agent card without initializing the full agent (like the Python sample)
    return new 
    { 
        name = "SK Travel Agent (.NET)",
        description = "Semantic Kernel-based travel agent providing comprehensive trip planning services",
        version = "1.0.0",
        capabilities = new { streaming = true },
        status = "ready"
    };
});

// Get host and port from configuration (Azure App Service will set PORT automatically)
// Log startup information (equivalent to Python logger.info)
app.Logger.LogInformation("Starting Semantic Kernel Travel Agent with A2A integration...");

app.Run();