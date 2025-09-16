targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('SKU name for the App Service plan')
param appServicePlanSku string = 'P0V3'

@description('Azure OpenAI deployment name')
param openAiDeploymentName string = 'gpt-4.1-mini'

@description('Azure OpenAI model name')
param openAiModelName string = 'gpt-4.1-mini'

@description('Azure OpenAI SKU name')
param openAiSkuName string = 'S0'

@description('Optional name for the App Service')
param appServiceName string = ''

// Variables
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// App Service Plan
module appServicePlan './core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: rg
  params: {
    name: '${abbrs.webServerfarms}${resourceToken}'
    location: location
    tags: tags
    sku: {
      name: appServicePlanSku
    }
  }
}

// Azure OpenAI
module openAi './core/ai/cognitiveservices.bicep' = {
  name: 'openai'
  scope: rg
  params: {
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    sku: {
      name: openAiSkuName
    }
    kind: 'OpenAI'
    deployments: [
      {
        name: openAiDeploymentName
        model: {
          format: 'OpenAI'
          name: openAiModelName
          version: '2025-04-14'
        }
        sku: {
          name: 'GlobalStandard'
          capacity: 250
        }
      }
    ]
  }
}

// Web App
module web './core/host/appservice.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: !empty(appServiceName) ? appServiceName : '${abbrs.webSitesAppService}web-${resourceToken}'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    runtimeName: 'dotnetcore'
    runtimeVersion: '9.0'
    tags: union(tags, { 'azd-service-name': 'web' })
    // Ensure the App Service does NOT run Oryx remote builds when we deploy a prebuilt package
    scmDoBuildDuringDeployment: false
    enableOryxBuild: false
    // Add app settings for Azure OpenAI configuration and run-from-package
    appSettings: {
      WEBSITE_RUN_FROM_PACKAGE: '1'
      AZURE_OPENAI_ENDPOINT: openAi.outputs.endpoint
      AZURE_OPENAI_DEPLOYMENT_NAME: openAiDeploymentName
      AZURE_OPENAI_API_VERSION: '2025-01-01-preview'
    }
  }
}

// Role assignment for App Service managed identity to access Azure OpenAI
module roleAssignment './core/security/role-assignment.bicep' = {
  name: 'openai-role-assignment'
  scope: rg
  params: {
    principalId: web.outputs.identityPrincipalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd' // Cognitive Services OpenAI User
    targetResourceId: openAi.outputs.id
  }
}

// Outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

output WEB_URI string = web.outputs.uri
output WEB_NAME string = web.outputs.name

output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
output AZURE_OPENAI_DEPLOYMENT_NAME string = openAiDeploymentName
