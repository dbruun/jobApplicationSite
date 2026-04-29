@description('Azure region for all resources.')
param location string

@description('Environment name prefix used to derive resource names (e.g. hhsc-jobportal-prod).')
param environmentName string

@description('SQL Server local administrator login (used for initial provisioning).')
param sqlAdminLogin string

@description('SQL Server local administrator password.')
@secure()
param sqlAdminPassword string

@description('Object ID (GUID) of the Entra ID user or group to set as the SQL Server Entra admin.')
param sqlEntraAdminObjectId string

@description('UPN or display name of the Entra ID user or group for the SQL Server Entra admin.')
param sqlEntraAdminLogin string

// ── Names ──────────────────────────────────────────────────────────────────────
var sqlServerName = '${environmentName}-sql'
var sqlDatabaseName = 'JobApplicationSiteDb'

// ── SQL Server ─────────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Entra ID admin ─────────────────────────────────────────────────────────────
resource sqlEntraAdmin 'Microsoft.Sql/servers/administrators@2023-05-01-preview' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: sqlEntraAdminLogin
    sid: sqlEntraAdminObjectId
    tenantId: tenant().tenantId
  }
}

// ── Enforce Azure AD-only authentication ──────────────────────────────────────
resource entraOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-05-01-preview' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
  dependsOn: [sqlEntraAdmin]
}

// ── Firewall: allow Azure-internal services (0.0.0.0 / 0.0.0.0) ──────────────
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── Database (Serverless General Purpose, auto-pause) ─────────────────────────
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB
    zoneRedundant: false
    licenseType: 'LicenseIncluded'
    minCapacity: json('0.5')
    autoPauseDelay: 60
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
@description('Fully-qualified domain name of the SQL Server.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Name of the SQL Server resource.')
output sqlServerName string = sqlServer.name

@description('Name of the SQL Database.')
output sqlDatabaseName string = sqlDatabase.name
