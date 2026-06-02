@description('Environment name (e.g. dev, staging, prod)')
param environmentName string = 'dev'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Application short name used in resource naming')
param appName string = 'contosouniv'

@description('Azure AD admin login name for SQL Server')
param aadAdminLogin string

@description('Azure AD admin object ID for SQL Server')
param aadAdminObjectId string

@description('Azure AD tenant ID')
param aadTenantId string

// ── Derived names ────────────────────────────────────────────────────────────
var uniqueStr = substring(uniqueString(resourceGroup().id), 0, 6)

var appServicePlanName  = 'asp-${appName}-${environmentName}'
var webAppName          = 'app-${appName}-${environmentName}-${uniqueStr}'
var sqlServerName       = 'sql-${appName}-${environmentName}-${uniqueStr}'
var sqlDatabaseName     = 'sqldb-${appName}-${environmentName}'
var serviceBusNsName    = 'sb-${appName}-${environmentName}-${uniqueStr}'
var storageAccountName  = toLower('st${replace(appName, '-', '')}${environmentName}${uniqueStr}')
var appConfigName       = 'appcs-${appName}-${environmentName}-${uniqueStr}'

var tags = {
  environment: environmentName
  application: appName
  managedBy: 'bicep'
}

// ── Modules ──────────────────────────────────────────────────────────────────

module appService 'modules/appservice.bicep' = {
  name: 'appServiceDeploy'
  params: {
    appServicePlanName: appServicePlanName
    webAppName: webAppName
    location: location
    tags: tags
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sqlDeploy'
  params: {
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    location: location
    tags: tags
    aadAdminLogin: aadAdminLogin
    aadAdminObjectId: aadAdminObjectId
    aadTenantId: aadTenantId
  }
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'serviceBusDeploy'
  params: {
    namespaceName: serviceBusNsName
    queueName: 'notifications'
    location: location
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storageDeploy'
  params: {
    storageAccountName: storageAccountName
    containerName: 'teaching-materials'
    location: location
    tags: tags
  }
}

module appConfig 'modules/appconfiguration.bicep' = {
  name: 'appConfigDeploy'
  params: {
    configStoreName: appConfigName
    location: location
    tags: tags
  }
}

module roleAssignments 'modules/roleassignments.bicep' = {
  name: 'roleAssignmentsDeploy'
  params: {
    webAppPrincipalId: appService.outputs.principalId
    serviceBusNamespaceId: serviceBus.outputs.namespaceId
    storageAccountId: storage.outputs.storageAccountId
    appConfigurationId: appConfig.outputs.configStoreId
  }
}

// ── App Settings ─────────────────────────────────────────────────────────────

resource webApp 'Microsoft.Web/sites@2023-01-01' existing = {
  name: webAppName
  dependsOn: [appService]
}

resource appSettings 'Microsoft.Web/sites/config@2023-01-01' = {
  name: 'appsettings'
  parent: webApp
  dependsOn: [roleAssignments]
  properties: {
    ASPNETCORE_ENVIRONMENT: environmentName == 'prod' ? 'Production' : 'Development'
    ConnectionStrings__DefaultConnection: 'Server=tcp:${sql.outputs.sqlServerFqdn};Database=${sqlDatabaseName};Authentication=Active Directory Default;'
    AzureServiceBus__FullyQualifiedNamespace: serviceBus.outputs.namespaceFqdn
    AzureServiceBus__QueueName: serviceBus.outputs.queueName
    Storage__ServiceUri: storage.outputs.blobEndpoint
    Storage__ContainerName: 'teaching-materials'
    AzureAppConfiguration__Endpoint: appConfig.outputs.endpoint
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

output webAppName string = webAppName
output webAppHostname string = appService.outputs.webAppHostname
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output sqlDatabaseName string = sqlDatabaseName
output serviceBusNamespaceFqdn string = serviceBus.outputs.namespaceFqdn
output serviceBusQueueName string = serviceBus.outputs.queueName
output storageAccountBlobEndpoint string = storage.outputs.blobEndpoint
output storageContainerName string = 'teaching-materials'
output appConfigurationEndpoint string = appConfig.outputs.endpoint
output webAppManagedIdentityPrincipalId string = appService.outputs.principalId
