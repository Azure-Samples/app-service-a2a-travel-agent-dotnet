# Semantic Kernel A2A Travel Agent

> **🔄 Adapted from Microsoft Sources**  
> This sample was adapted from the [Microsoft DevBlogs Semantic Kernel A2A Integration article](https://devblogs.microsoft.com/foundry/semantic-kernel-a2a-integration/) and the [A2A Samples repository](https://github.com/a2aproject/a2a-samples/tree/main/samples/python/agents/semantickernel) to run as a single standalone web application on Azure App Service with a modern web interface.

A standalone web application that combines Semantic Kernel AI agents with Google's Agent-to-Agent (A2A) protocol to provide comprehensive travel planning services. This application features a modern web interface and is designed for deployment on Azure App Service.

This is a .NET replica of the Azure Travel Agent sample application. The application provides an AI-powered travel planning assistant using Azure OpenAI and the Semantic Kernel framework. For more details, see the original Python implementation: https://github.com/Azure-Samples/app-service-a2a-travel-agent.

## Prerequisites

1. Azure CLI installed and configured
2. Azure subscription with access to Azure OpenAI
3. .NET 9.0 SDK installed
4. Azure Developer CLI (azd) installed

## Quick Deployment

1. Initialize azd environment:
   ```bash
   azd init
   ```

2. Provision infrastructure and deploy:
   ```bash
   azd up
   ```

This will:
- Create all necessary Azure resources (App Service, OpenAI, managed identity)
- Configure role assignments for secure access
- Deploy the .NET application

## Configuration

The application uses managed identity for secure access to Azure OpenAI. No API keys are stored in configuration.

## Features

- Multi-agent conversation orchestration
- Real-time currency exchange rates
- Activity planning and recommendations
- Streaming chat responses
- Session management
- Agent discovery via A2A protocol

## Architecture

- **Frontend**: HTML/CSS/JavaScript single-page application
- **Backend**: ASP.NET Core Web API with Semantic Kernel
- **AI**: Azure OpenAI (GPT models)
- **Infrastructure**: Azure App Service with managed identity
- **Authentication**: Managed Identity for Azure resource access

## Local Development

### ⚠️ Prerequisites for Chat Functionality

**The chat will not work without Azure OpenAI configured first!** You need:

1. **Deploy an Azure OpenAI resource** in your Azure subscription
2. **Deploy a GPT model** (e.g., `gpt-4`, `gpt-35-turbo`) in your OpenAI resource  
3. **Set the required environment variables** (see below)

If you deploy the provided azd template, these will be set automatically in your App Service and you can find the environment variables in the Azure Portal under your App Service's Environment Variables. These needed values are in the following section.

### Running Locally

1. **Set environment variables** with your Azure OpenAI details:
   ```bash
   export AZURE_OPENAI_ENDPOINT="https://your-openai-resource.openai.azure.com/"
   export AZURE_OPENAI_DEPLOYMENT_NAME="your-gpt-model-deployment-name"  
   export AZURE_OPENAI_API_VERSION="2025-04-14"
   ```

2. **Ensure you're authenticated** with Azure CLI (for managed identity):
   ```bash
   az login
   ```

3. **Run the application**:
   ```bash
   cd src/TravelAgent.Api
   dotnet run
   ```

4. **Access the app** at `http://localhost:5000`

## API Endpoints

- `GET /` - Serves the web interface
- `POST /api/chat` - Chat completion endpoint
- `GET /api/health` - Health check endpoint
- `GET /api/agents` - Agent discovery (A2A protocol)

## Troubleshooting

### Local Development Issues

1. **"AZURE_OPENAI_ENDPOINT is required" error**: 
   - Set the environment variables listed in Local Development section
   - Make sure you've deployed an Azure OpenAI resource first

2. **Chat shows "Sorry, I encountered an error"**: 
   - Verify your Azure OpenAI deployment name matches `AZURE_OPENAI_DEPLOYMENT_NAME`
   - Check that you're authenticated (`az login`) if not using API key
   - Ensure your Azure account has access to the OpenAI resource

3. **App starts but chat doesn't respond**: 
   - Check browser developer console for errors
   - Verify the API version is compatible with your Azure OpenAI deployment

### Deployment Issues

1. **401 Unauthorized errors**: Ensure managed identity has "Cognitive Services OpenAI User" role
2. **Deployment failures**: Check that Azure OpenAI is available in your region  
3. **Build errors**: Ensure .NET 9.0 SDK is installed

For more details, see the original Python implementation: https://github.com/Azure-Samples/app-service-a2a-travel-agent