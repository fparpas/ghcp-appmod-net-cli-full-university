@description('Name prefix used for all resources')
param appName string = 'contoso-uni'

@description('Short environment identifier (e.g., prod, dev, staging)')
param environmentName string = 'prod'

@description('Azure region for all resources')
param location string = 'swedencentral'

@description('Object ID of the Azure AD user/service principal to set as SQL Server Entra admin')
param sqlEntraAdminObjectId string

@description('Display name / UPN of the Azure AD SQL Server admin')
param sqlEntraAdminLogin string

// ---------------------------------------------------------------------------
// Shared variables
// ---------------------------------------------------------------------------
var uniqueSuffix = substring(uniqueString(resourceGroup().id), 0, 8)

var tags = {
  application: appName
  environment: environmentName
  managedBy: 'bicep'
  project: 'ContosoUniversity'
}

// ---------------------------------------------------------------------------
// Modules
// ---------------------------------------------------------------------------

module appServiceMod 'modules/appservice.bicep' = {
  name: 'deploy-appservice'
  params: {
    appName: appName
    environmentName: environmentName
    location: location
    uniqueSuffix: uniqueSuffix
    tags: tags
  }
}

module sqlMod 'modules/sql.bicep' = {
  name: 'deploy-sql'
  params: {
    appName: appName
    location: location
    uniqueSuffix: uniqueSuffix
    sqlEntraAdminObjectId: sqlEntraAdminObjectId
    sqlEntraAdminLogin: sqlEntraAdminLogin
    tags: tags
  }
}

module serviceBusMod 'modules/servicebus.bicep' = {
  name: 'deploy-servicebus'
  params: {
    appName: appName
    location: location
    uniqueSuffix: uniqueSuffix
    tags: tags
  }
}

module storageMod 'modules/storage.bicep' = {
  name: 'deploy-storage'
  params: {
    appName: appName
    location: location
    uniqueSuffix: uniqueSuffix
    tags: tags
  }
}

module appConfigMod 'modules/appconfig.bicep' = {
  name: 'deploy-appconfig'
  params: {
    appName: appName
    location: location
    uniqueSuffix: uniqueSuffix
    tags: tags
  }
}

module roleAssignmentsMod 'modules/roleassignments.bicep' = {
  name: 'deploy-roleassignments'
  params: {
    appServicePrincipalId: appServiceMod.outputs.principalId
    serviceBusNamespaceName: serviceBusMod.outputs.namespaceName
    storageAccountName: storageMod.outputs.storageAccountName
    appConfigName: appConfigMod.outputs.appConfigName
  }
}

module appSettingsMod 'modules/appsettings.bicep' = {
  name: 'deploy-appsettings'
  params: {
    appServiceName: appServiceMod.outputs.appServiceName
    appConfigEndpoint: appConfigMod.outputs.endpoint
    serviceBusFqdn: serviceBusMod.outputs.namespaceFqdn
    serviceBusQueueName: serviceBusMod.outputs.queueName
    storageUri: storageMod.outputs.storageAccountUri
    storageContainerName: storageMod.outputs.blobContainerName
    sqlServerFqdn: sqlMod.outputs.sqlServerFqdn
    sqlDatabaseName: sqlMod.outputs.sqlDatabaseName
  }
  dependsOn: [
    roleAssignmentsMod
  ]
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output appServiceName string = appServiceMod.outputs.appServiceName
output appServiceHostName string = appServiceMod.outputs.appServiceHostName
output appServicePrincipalId string = appServiceMod.outputs.principalId

output sqlServerName string = sqlMod.outputs.sqlServerName
output sqlServerFqdn string = sqlMod.outputs.sqlServerFqdn
output sqlDatabaseName string = sqlMod.outputs.sqlDatabaseName

output serviceBusNamespaceName string = serviceBusMod.outputs.namespaceName
output serviceBusNamespaceFqdn string = serviceBusMod.outputs.namespaceFqdn
output serviceBusQueueName string = serviceBusMod.outputs.queueName

output storageAccountName string = storageMod.outputs.storageAccountName
output storageAccountUri string = storageMod.outputs.storageAccountUri
output blobContainerName string = storageMod.outputs.blobContainerName

output appConfigName string = appConfigMod.outputs.appConfigName
output appConfigEndpoint string = appConfigMod.outputs.endpoint
