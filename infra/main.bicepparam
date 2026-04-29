/*
  main.bicepparam — example parameter values for main.bicep
  ==========================================================
  Copy this file and fill in your real values before deploying.
  Sensitive values (sqlAdminPassword) should be supplied at deploy time
  via --parameters or sourced from Azure Key Vault references — do NOT
  commit real secrets here.

  Deploy example:
    az deployment group create \
      --resource-group <your-rg> \
      --template-file infra/main.bicep \
      --parameters infra/main.bicepparam \
      --parameters sqlAdminPassword='<secret>'
*/

using 'main.bicep'

// Short environment prefix — drives all resource names.
param environmentName = 'hhsc-jobportal-prod'

// Azure region (leave empty to inherit from resource group, or override here).
param location = 'eastus'

// SQL local admin — only used during initial provisioning.
// Entra-only authentication is enforced by the template.
param sqlAdminLogin = 'sqladmin'
param sqlAdminPassword = ''   // supply at deploy time, never commit here

// Entra ID admin for SQL Server.
// Find the object ID: az ad user show --id user@domain.com --query id -o tsv
param sqlEntraAdminObjectId = ''  // e.g. '00000000-0000-0000-0000-000000000000'
param sqlEntraAdminLogin = ''     // e.g. 'admin@yourdomain.onmicrosoft.com'

// Principal ID of the Container App's managed identity.
// Find it after creating the Container App:
//   az containerapp show -n <app> -g <rg> --query identity.principalId -o tsv
// Leave empty on first deployment (before the Container App exists) and
// re-run the template once you have the principal ID.
param containerAppPrincipalId = ''
