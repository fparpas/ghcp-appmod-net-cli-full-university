@description('App Service Plan name')
param appServicePlanName string

@description('Web App name')
param webAppName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object = {}

@description('App Service Plan SKU')
param sku object = {
  name: 'B2'
  tier: 'Basic'
  size: 'B2'
  family: 'B'
  capacity: 1
}

// ── App Service Plan ──────────────────────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  kind: 'linux'
  sku: sku
  properties: {
    reserved: true  // required for Linux
  }
}

// ── Web App ───────────────────────────────────────────────────────────────────

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
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
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output principalId string = webApp.identity.principalId
output webAppHostname string = webApp.properties.defaultHostName
output webAppId string = webApp.id
output webAppName string = webApp.name
