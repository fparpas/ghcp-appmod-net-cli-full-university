@description('SQL Server name')
param sqlServerName string

@description('SQL Database name')
param sqlDatabaseName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object = {}

@description('Azure AD admin display name for SQL Server')
param aadAdminLogin string

@description('Azure AD admin object ID for SQL Server')
param aadAdminObjectId string

@description('Azure AD tenant ID')
param aadTenantId string

@description('Database SKU name')
param skuName string = 'GP_S_Gen5_2'

@description('Database service tier')
param tier string = 'GeneralPurpose'

// ── SQL Server ────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      login: aadAdminLogin
      sid: aadAdminObjectId
      tenantId: aadTenantId
      azureADOnlyAuthentication: true
    }
  }
}

// ── Firewall: allow Azure services ───────────────────────────────────────────

resource firewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  name: 'AllowAllAzureIPs'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── SQL Database ──────────────────────────────────────────────────────────────

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  name: sqlDatabaseName
  parent: sqlServer
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: tier
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    autoPauseDelay: 60
    minCapacity: 1
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerId string = sqlServer.id
output sqlDatabaseId string = sqlDatabase.id
