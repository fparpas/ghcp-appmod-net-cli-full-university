@description('App name prefix')
param appName string

@description('Environment name (prod, dev, staging)')
param environmentName string

@description('Azure region')
param location string

@description('Unique suffix for globally unique resource names')
param uniqueSuffix string

@description('Resource tags')
param tags object

// ---------------------------------------------------------------------------
// Variables
// ---------------------------------------------------------------------------
var appServicePlanName = 'asp-${appName}-${environmentName}'
var appServiceName = 'app-${appName}-${uniqueSuffix}'

// ---------------------------------------------------------------------------
// App Service Plan (Linux, B2 Basic)
// ---------------------------------------------------------------------------
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B2'
    tier: 'Basic'
    size: 'B2'
    family: 'B'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true   // required for Linux plans
  }
}

// ---------------------------------------------------------------------------
// App Service (.NET 10, System-Assigned Managed Identity)
// ---------------------------------------------------------------------------
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET|10.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output appServicePlanName string = appServicePlan.name
output appServiceName string = appService.name
output appServiceHostName string = appService.properties.defaultHostName
output principalId string = appService.identity.principalId
output appServiceId string = appService.id
