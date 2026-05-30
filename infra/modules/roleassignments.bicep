@description('Principal ID of the App Service System-Assigned Managed Identity')
param appServicePrincipalId string

@description('Service Bus namespace name')
param serviceBusNamespaceName string

@description('Storage account name')
param storageAccountName string

@description('App Configuration store name')
param appConfigName string

// ---------------------------------------------------------------------------
// Built-in role definition IDs
// ---------------------------------------------------------------------------
// Azure Service Bus Data Owner  → full send/receive/manage on Service Bus
var serviceBusDataOwnerRoleId    = '090c5cfd-751d-490a-894a-3ce6f1109419'
// Storage Blob Data Contributor → read/write blobs
var storageBlobDataContributorId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
// App Configuration Data Reader → read key-values from App Config
var appConfigDataReaderRoleId    = '516239f1-63e1-4d78-a4de-a74fb236a071'

// ---------------------------------------------------------------------------
// Existing resource references (for scoped role assignments)
// ---------------------------------------------------------------------------
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = {
  name: serviceBusNamespaceName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource appConfigStore 'Microsoft.AppConfiguration/configurationStores@2023-03-01' existing = {
  name: appConfigName
}

// ---------------------------------------------------------------------------
// Role assignment: App Service MI → Azure Service Bus Data Owner
// ---------------------------------------------------------------------------
resource serviceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, appServicePrincipalId, serviceBusDataOwnerRoleId)
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataOwnerRoleId)
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------
// Role assignment: App Service MI → Storage Blob Data Contributor
// ---------------------------------------------------------------------------
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, appServicePrincipalId, storageBlobDataContributorId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorId)
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------
// Role assignment: App Service MI → App Configuration Data Reader
// ---------------------------------------------------------------------------
resource appConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigStore.id, appServicePrincipalId, appConfigDataReaderRoleId)
  scope: appConfigStore
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', appConfigDataReaderRoleId)
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}
