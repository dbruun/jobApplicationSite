/*
  main.bicep — HHSC Job Application Portal infrastructure
  =========================================================
  Deploys:
    • Azure SQL Server + Database  (Entra-only auth, serverless GP tier)
    • Azure Blob Storage Account   (resumes container, TLS 1.2+, no public access)
    • RBAC: Storage Blob Data Contributor → Container App managed identity

  NOTE — SQL managed-identity access:
    After deploying this template, run the following SQL commands once against
    the database to grant the Container App's managed identity access:

      CREATE USER [<container-app-name>] FROM EXTERNAL PROVIDER;
      ALTER ROLE db_datareader ADD MEMBER [<container-app-name>];
      ALTER ROLE db_datawriter ADD MEMBER [<container-app-name>];
      ALTER ROLE db_ddladmin  ADD MEMBER [<container-app-name>];

    The Container App must have a system-assigned (or user-assigned) managed
    identity and its display name must match <container-app-name> above.
*/

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short environment prefix used to derive all resource names (e.g. hhsc-jobportal-prod). Must be lowercase, alphanumeric with hyphens.')
@minLength(3)
@maxLength(24)
param environmentName string

@description('SQL Server local administrator login (used only for initial provisioning; Entra-only auth is enforced afterwards).')
param sqlAdminLogin string

@description('SQL Server local administrator password.')
@secure()
param sqlAdminPassword string

@description('Object ID (GUID) of the Entra ID user or group that becomes the SQL Server Entra administrator.')
param sqlEntraAdminObjectId string

@description('UPN or display name of the Entra ID user or group for the SQL Entra administrator.')
param sqlEntraAdminLogin string

@description('Principal ID of the Container App managed identity. Used to assign Storage Blob Data Contributor on the storage account. Leave empty (\'\') to skip the RBAC assignment (e.g. before the Container App exists).')
param containerAppPrincipalId string = ''

// ── SQL module ────────────────────────────────────────────────────────────────

module sql 'modules/sql.bicep' = {
  name: 'sql-deployment'
  params: {
    location: location
    environmentName: environmentName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    sqlEntraAdminObjectId: sqlEntraAdminObjectId
    sqlEntraAdminLogin: sqlEntraAdminLogin
  }
}

// ── Storage module ────────────────────────────────────────────────────────────

module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  params: {
    location: location
    environmentName: environmentName
    containerAppPrincipalId: containerAppPrincipalId
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Fully-qualified domain name of the Azure SQL Server.')
output sqlServerFqdn string = sql.outputs.sqlServerFqdn

@description('Name of the Azure SQL Database.')
output sqlDatabaseName string = sql.outputs.sqlDatabaseName

@description('Ready-to-use connection string for the application (Managed Identity / Active Directory Default auth).')
output connectionString string = 'Server=tcp:${sql.outputs.sqlServerFqdn},1433;Database=${sql.outputs.sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

@description('Blob service endpoint URI — set as AzureStorage:AccountUri in app configuration.')
output storageAccountUri string = storage.outputs.storageAccountUri

@description('Name of the resumes blob container.')
output resumesContainerName string = storage.outputs.resumesContainerName
