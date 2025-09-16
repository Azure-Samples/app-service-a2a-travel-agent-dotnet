param name string
param location string = resourceGroup().location
param tags object = {}

param appServicePlanId string
param runtimeName string
param runtimeVersion string
param appSettings object = {}

@description('Whether the App Service should run Oryx build during zip deployment')
param scmDoBuildDuringDeployment bool = false

@description('Whether to enable Oryx build at all (set false for prebuilt packages)')
param enableOryxBuild bool = false

var effectiveAppSettings = union(appSettings, {
  SCM_DO_BUILD_DURING_DEPLOYMENT: string(scmDoBuildDuringDeployment)
  ENABLE_ORYX_BUILD: string(enableOryxBuild)
  WEBSITE_RUN_FROM_PACKAGE: '1'
})

var linuxFxVersion = '${toUpper(runtimeName)}|${runtimeVersion}'

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      appSettings: [for item in items(effectiveAppSettings): {
        name: item.key
        value: item.value
      }]
    }
    httpsOnly: true
  }
}

output id string = appService.id
output name string = appService.name
output uri string = 'https://${appService.properties.defaultHostName}'
output identityPrincipalId string = appService.identity.principalId
