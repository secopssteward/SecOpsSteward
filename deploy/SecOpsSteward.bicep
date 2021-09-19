targetScope = 'subscription'

// ---

@description('Resource Group')
param resourceGroupName string

@description('A random string to append to all resources in deployment')
param deploymentId string = uniqueString(resourceGroupName)

@description('Azure Active Directory Application ID')
param azureAdApplicationId string

@description('Azure Active Directory Application Secret')
param azureAdApplicationSecret string

@description('Include Debug Roles')
param includeDebugRoles bool = false

@description('SecOps Steward Web App Name')
param webAppName string

// Web App params
@allowed([
  'F1'
  'D1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1'
  'P2'
  'P3'
  'P4'
])
@description('Web App Service SKU')
param skuName string = 'F1'

@minValue(1)
@description('Number of Web App instances')
param skuCapacity int = 1

@description('Key Vault Service SKU')
param vaultSku string = 'Standard'

@description('SQL Server Administrator Username')
param sqlAdministratorLogin string

@minLength(12)
@maxLength(128)
@description('SQL Server Administrator Password')
@secure()
param sqlAdministratorLoginPassword string

@description('Administrator User Principal')
param mainUserPrincipal string

// ---

var signDecryptRoleName = 'SoS Key Sign-Only User'
var verifyEncryptRoleName = 'SoS Key Verify-Only User'
var resourceGroupLocation = deployment().location

// ---

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-01-01' = {
  name: resourceGroupName
  location: resourceGroupLocation
}



// --------------------------==[ KEY VAULT ROLES ]==--------------------------
resource keyvault_roles_signdecrypt 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' = {
  name: guid(signDecryptRoleName)
  properties: {
    roleName: signDecryptRoleName
    description: 'User can only sign with a key'
    permissions: [
      {
        dataActions: [
          'Microsoft.KeyVault/vaults/keys/read'
          'Microsoft.KeyVault/vaults/keys/unwrap/action'
          'Microsoft.KeyVault/vaults/keys/decrypt/action'
          'Microsoft.KeyVault/vaults/keys/sign/action'
        ]
      }
    ]
    assignableScopes: [
      subscription().id
    ]
  }
}

resource keyvault_roles_verifyencrypt 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' = {
  name: guid(verifyEncryptRoleName)
  properties: {
    roleName: verifyEncryptRoleName
    description: 'User can only verify with a key'
    permissions: [
      {
        dataActions: [
          'Microsoft.KeyVault/vaults/keys/read'
          'Microsoft.KeyVault/vaults/keys/wrap/action'
          'Microsoft.KeyVault/vaults/keys/encrypt/action'
          'Microsoft.KeyVault/vaults/keys/verify/action'
        ]
      }
    ]
    assignableScopes: [
      subscription().id
    ]
  }
}



module resourceGroupModule './SecOpsStewardRG.bicep' = {
  name: 'rgResources'
  scope: resourceGroup
  params: {
    randomString: deploymentId
    includeDebugRoles: includeDebugRoles
    aadAppSecret: azureAdApplicationSecret
    mainUserPrincipal: mainUserPrincipal
    resourceGroupName: resourceGroup.name
    signDecryptRole: guid(signDecryptRoleName)
    verifyEncryptRole: guid(verifyEncryptRoleName)
    subscriptionId: subscription().subscriptionId
    aadAppId: azureAdApplicationId
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
    skuCapacity: skuCapacity
    skuName: skuName
    vaultSku: vaultSku
    webAppName: webAppName
  }
}

output signDecryptGuid string = guid(signDecryptRoleName)
output verifyEncryptGuid string = guid(verifyEncryptRoleName)
