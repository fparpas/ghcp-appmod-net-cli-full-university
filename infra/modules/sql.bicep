@description('App name prefix')
param appName string

@description('Azure region')
param location string

@description('Unique suffix for globally unique resource names')
param uniqueSuffix string

@description('Object ID of the Azure AD user/service principal to set as SQL Server Entra admin (required for Entra-only auth)')
param sqlEntraAdminObjectId string

@description('Display name / UPN of the Azure AD SQL Server admin')
param sqlEntraAdminLogin string

@description('Resource tags')
param tags object

// ---------------------------------------------------------------------------
// Variables
// ---------------------------------------------------------------------------
var sqlServerName = 'sql-${appName}-${uniqueSuffix}'
var sqlDatabaseName = 'ContosoUniversityDB'

// ---------------------------------------------------------------------------
// SQL Server — Entra-only authentication (required by MCAPS policy)
// ---------------------------------------------------------------------------
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    // Entra-only: no SQL admin login/password required
    administrators: {
      administratorType: 'ActiveDirectory'
      login: sqlEntraAdminLogin
      sid: sqlEntraAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
}

// Allow Azure services (App Service, etc.) to reach the SQL Server
resource sqlFirewallAzureServices 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------------------------------------------------------------------------
// SQL Database (Standard S1)
// ---------------------------------------------------------------------------
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648   // 2 GB
    readScale: 'Disabled'
    zoneRedundant: false
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output sqlServerId string = sqlServer.id
