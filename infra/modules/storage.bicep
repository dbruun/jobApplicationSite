@description('Azure region for all resources.')
param location string

@description('Environment name prefix used to derive resource names (e.g. hhsc-jobportal-prod).')
param environmentName string

@description('Principal ID of the Container App managed identity that needs access to Blob Storage. Leave empty to skip RBAC assignment.')
param containerAppPrincipalId string = ''

// ── Names ──────────────────────────────────────────────────────────────────────
// Storage account names: lowercase, alphanumeric, max 24 chars.
var storageAccountName = toLower(take('${replace(replace(environmentName, '-', ''), '_', '')}stor', 24))

var resumesContainerName = 'resumes'

// Built-in role: Storage Blob Data Contributor
// https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// ── Storage Account ───────────────────────────────────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    // Shared key access is left enabled for local-dev fallback; set to false
    // in high-security environments where only Managed Identity is used.
    allowSharedKeyAccess: true
  }
}

// ── Blob service (required parent for container) ──────────────────────────────
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// ── resumes container ─────────────────────────────────────────────────────────
resource resumesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: resumesContainerName
  properties: {
    publicAccess: 'None'
  }
}

// ── RBAC: Storage Blob Data Contributor → Container App managed identity ──────
resource storageBlobContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(containerAppPrincipalId)) {
  // Deterministic GUID scoped to storage account + principal + role
  name: guid(storageAccount.id, containerAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      storageBlobDataContributorRoleId
    )
    principalId: containerAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
@description('Blob service endpoint URI (use as AzureStorage:AccountUri in app config).')
output storageAccountUri string = storageAccount.properties.primaryEndpoints.blob

@description('Name of the storage account resource.')
output storageAccountName string = storageAccount.name

@description('Name of the resumes blob container.')
output resumesContainerName string = resumesContainer.name
