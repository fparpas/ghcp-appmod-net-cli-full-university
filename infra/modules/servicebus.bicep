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
var namespaceName = 'sb-${appName}-${uniqueSuffix}'
var queueName = 'contoso-university-notifications'

// ---------------------------------------------------------------------------
// Service Bus Namespace (Standard tier)
// ---------------------------------------------------------------------------
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    disableLocalAuth: false
  }
}

// ---------------------------------------------------------------------------
// Notification queue
// ---------------------------------------------------------------------------
resource notificationsQueue 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: serviceBusNamespace
  name: queueName
  properties: {
    lockDuration: 'PT1M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
    enableBatchedOperations: true
    enableExpress: false
    enablePartitioning: false
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output namespaceName string = serviceBusNamespace.name
output namespaceFqdn string = '${serviceBusNamespace.name}.servicebus.windows.net'
output queueName string = notificationsQueue.name
output namespaceId string = serviceBusNamespace.id
