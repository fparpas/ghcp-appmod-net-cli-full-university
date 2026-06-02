@description('Service Bus namespace name')
param namespaceName string

@description('Queue name')
param queueName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object = {}

@description('Service Bus SKU')
param skuName string = 'Standard'

// ── Service Bus Namespace ─────────────────────────────────────────────────────

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

// ── Queue ─────────────────────────────────────────────────────────────────────

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: queueName
  parent: serviceBusNamespace
  properties: {
    lockDuration: 'PT1M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
    enablePartitioning: false
    enableExpress: false
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output namespaceId string = serviceBusNamespace.id
output namespaceFqdn string = '${namespaceName}.servicebus.windows.net'
output queueName string = queue.name
