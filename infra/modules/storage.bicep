@description('App name prefix')
param appName string

@description('Azure region')
param location string

@description('Unique suffix for globally unique resource names (exactly 8 characters)')
@minLength(8)
@maxLength(8)
param uniqueSuffix string

@description('Resource tags')
param tags object

// ---------------------------------------------------------------------------
// Variables
// Storage account name constraints: 3-24 chars, lowercase letters and numbers only
// 'st' (2) + appName without hyphens (max 11) + uniqueSuffix (8) = max 21 chars
// ---------------------------------------------------------------------------
var storageAccountName = 'st${toLower(replace(appName, '-', ''))}${uniqueSuffix}'
var blobContainerName = 'teaching-materials'

// ---------------------------------------------------------------------------
// Storage Account (StorageV2, Standard LRS)
// ---------------------------------------------------------------------------
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowSharedKeyAccess: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ---------------------------------------------------------------------------
// Blob Service settings (soft-delete enabled, 7-day retention)
// ---------------------------------------------------------------------------
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

// ---------------------------------------------------------------------------
// Blob Container for teaching materials (private, no public access)
// ---------------------------------------------------------------------------
resource teachingMaterialsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: blobContainerName
  properties: {
    publicAccess: 'None'
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output storageAccountName string = storageAccount.name
output storageAccountUri string = storageAccount.properties.primaryEndpoints.blob
output blobContainerName string = teachingMaterialsContainer.name
output storageAccountId string = storageAccount.id
