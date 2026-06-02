@description('Principal ID of the Web App managed identity')
param webAppPrincipalId string

@description('Resource ID of the Service Bus namespace')
param serviceBusNamespaceId string

@description('Resource ID of the Storage account')
param storageAccountId string

@description('Resource ID of the App Configuration store')
param appConfigurationId string

// ── Role Definition IDs ───────────────────────────────────────────────────────
// Azure Service Bus Data Sender
var serviceBusDataSenderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
// Azure Service Bus Data Receiver
var serviceBusDataReceiverRoleId = '4f6d3b9f-4099-4b3c-ad6b-d7e3f2c3e8b2'
// Storage Blob Data Contributor
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
// App Configuration Data Reader
var appConfigDataReaderRoleId = '516239f1-63e1-4d78-a4de-a74fb236a071'

// ── Service Bus: Data Sender ──────────────────────────────────────────────────

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: last(split(serviceBusNamespaceId, '/'))
}

resource sbSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespaceId, webAppPrincipalId, serviceBusDataSenderRoleId)
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataSenderRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Service Bus: Data Receiver ────────────────────────────────────────────────

resource sbReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespaceId, webAppPrincipalId, serviceBusDataReceiverRoleId)
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataReceiverRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Storage: Blob Data Contributor ───────────────────────────────────────────

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: last(split(storageAccountId, '/'))
}

resource storageBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, webAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── App Configuration: Data Reader ───────────────────────────────────────────

resource appConfigStore 'Microsoft.AppConfiguration/configurationStores@2023-03-01' existing = {
  name: last(split(appConfigurationId, '/'))
}

resource appConfigReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigurationId, webAppPrincipalId, appConfigDataReaderRoleId)
  scope: appConfigStore
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', appConfigDataReaderRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}
