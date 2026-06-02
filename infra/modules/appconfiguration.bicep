@description('App Configuration store name')
param configStoreName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object = {}

@description('App Configuration SKU')
param skuName string = 'free'

// ── App Configuration ─────────────────────────────────────────────────────────

resource appConfigStore 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: configStoreName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    disableLocalAuth: false
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output configStoreId string = appConfigStore.id
output endpoint string = appConfigStore.properties.endpoint
