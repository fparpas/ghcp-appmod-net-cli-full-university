@description('App name prefix')
param appName string

@description('Azure region')
param location string

@description('Unique suffix for globally unique resource names')
param uniqueSuffix string

@description('Resource tags')
param tags object

// ---------------------------------------------------------------------------
// Variables
// ---------------------------------------------------------------------------
var appConfigName = 'appcs-${appName}-${uniqueSuffix}'

// ---------------------------------------------------------------------------
// Azure App Configuration (Standard tier)
// ---------------------------------------------------------------------------
resource appConfigStore 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigName
  location: location
  tags: tags
  sku: {
    name: 'standard'
  }
  properties: {
    disableLocalAuth: false
    enablePurgeProtection: false
    softDeleteRetentionInDays: 1
    publicNetworkAccess: 'Enabled'
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output appConfigName string = appConfigStore.name
output endpoint string = appConfigStore.properties.endpoint
output appConfigId string = appConfigStore.id
