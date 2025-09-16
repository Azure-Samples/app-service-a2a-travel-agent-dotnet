# Deployment Instructions

This is a .NET replica of the Azure Travel Agent sample application. The application provides an AI-powered travel planning assistant using Azure OpenAI and the Semantic Kernel framework.

## Prerequisites

1. Azure CLI installed and configured
2. Azure subscription with access to Azure OpenAI
3. .NET 8.0 SDK installed
4. Azure Developer CLI (azd) installed

## Quick Deployment

### Option 1: Using Azure Developer CLI (azd)

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

### Option 2: Manual Deployment

1. Deploy infrastructure:
   ```bash
   az deployment group create \
     --resource-group <your-resource-group> \
     --template-file infra/main.bicep \
     --parameters environmentName=<env-name> location=<location>
   ```

2. Build and deploy the application:
   ```bash
   dotnet publish src/TravelAgent.Api/TravelAgent.Api.csproj -c Release -o publish
   az webapp deployment source config-zip \
     --resource-group <your-resource-group> \
     --name <app-service-name> \
     --src publish.zip
   ```

## Configuration

The application uses managed identity for secure access to Azure OpenAI. No API keys are stored in configuration.

Required app settings (automatically configured via Bicep):
- `AZURE_OPENAI_ENDPOINT`: Azure OpenAI service endpoint
- `AZURE_CLIENT_ID`: Managed identity client ID (optional)

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

1. Set up local configuration in `appsettings.Development.json`:
   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "https://your-openai.openai.azure.com/",
       "DeploymentName": "gpt-35-turbo"
     }
   }
   ```

2. Ensure you're authenticated with Azure CLI:
   ```bash
   az login
   ```

3. Run the application:
   ```bash
   cd src/TravelAgent.Api
   dotnet run
   ```

The application will be available at `http://localhost:5000`.

## API Endpoints

- `GET /` - Serves the web interface
- `POST /api/chat` - Chat completion endpoint
- `GET /api/health` - Health check endpoint
- `GET /api/agents` - Agent discovery (A2A protocol)

## Troubleshooting

1. **401 Unauthorized errors**: Ensure managed identity has "Cognitive Services OpenAI User" role
2. **Deployment failures**: Check that Azure OpenAI is available in your region
3. **Build errors**: Ensure .NET 8.0 SDK is installed

For more details, see the original Python implementation: https://github.com/Azure-Samples/app-service-a2a-travel-agent