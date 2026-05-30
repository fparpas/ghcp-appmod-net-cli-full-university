@description('App Service name to configure')
param appServiceName string

@description('Azure App Configuration endpoint')
param appConfigEndpoint string

@description('Service Bus fully qualified namespace (e.g. sb-name.servicebus.windows.net)')
param serviceBusFqdn string

@description('Service Bus queue name')
param serviceBusQueueName string

@description('Storage account blob service URI')
param storageUri string

@description('Blob container name for teaching materials')
param storageContainerName string

@description('SQL Server FQDN')
param sqlServerFqdn string

@description('SQL Database name')
param sqlDatabaseName string

// ---------------------------------------------------------------------------
// Reference existing App Service
// ---------------------------------------------------------------------------
resource appService 'Microsoft.Web/sites@2023-01-01' existing = {
  name: appServiceName
}

// ---------------------------------------------------------------------------
// Apply application settings
// ---------------------------------------------------------------------------
resource appServiceConfig 'Microsoft.Web/sites/config@2023-01-01' = {
  name: 'appsettings'
  parent: appService
  properties: {
    AZURE_APP_CONFIGURATION_ENDPOINT: appConfigEndpoint
    AzureServiceBus__FullyQualifiedNamespace: serviceBusFqdn
    AzureServiceBus__QueueName: serviceBusQueueName
    Storage__ServiceUri: storageUri
    Storage__ContainerName: storageContainerName
    ConnectionStrings__DefaultConnection: 'Server=tcp:${sqlServerFqdn};Database=${sqlDatabaseName};Authentication=Active Directory Default;TrustServerCertificate=True'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    SCM_DO_BUILD_DURING_DEPLOYMENT: 'false'
  }
}
