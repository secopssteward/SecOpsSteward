// Deploy SecOps Steward Web App and Core Platform Services

// TODO List:
// - Migrate SQL password to Managed Identity once MI doesn't require running SQL commands

// ----------------------------------------------------------------------------

@description('Azure Active Directory Application ID')
param aadAppId string
@description('Azure Active Directory Application Secret')
param aadAppSecret string
@description('Administrator User Principal')
param mainUserPrincipal string
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

@description('Subscription ID')
param subscriptionId string

@description('Sign/Decrypt-Only Key Vault Role ID')
param signDecryptRole string

@description('Verify/Encrypt-Only Key Vault Role ID')
param verifyEncryptRole string

param resourceGroupName string

@description('Include debug roles for mainUserPrincipal user')
param includeDebugRoles bool = false

@description('Random string to append to all resources in deployment')
param randomString string = uniqueString(resourceGroup().name)

// ----------------------------------------------------------------------------
var location = resourceGroup().location

var hostingPlanName = 'soshosting${randomString}'
var sqlserverName = 'sossql${randomString}'
var databaseName = 'sosdb'
var userVaultName = 'sosuvault${randomString}'
var agentVaultName = 'sosavault${randomString}'
var serviceBusNamespaceName = 'sosbus${randomString}'
var storageAccountName = 'sosblobs${randomString}'
// ----------------------------------------------------------------------------

// ------------------------------==[ SQL ]==------------------------------
resource sqlserver 'Microsoft.Sql/servers@2019-06-01-preview' = {
  name: sqlserverName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    version: '12.0'
  }
}

resource sqlserver_database 'Microsoft.Sql/servers/databases@2020-08-01-preview' = {
  name: '${sqlserver.name}/${databaseName}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 1073741824
  }
}

resource sqlserver_allowazureips 'Microsoft.Sql/servers/firewallRules@2014-04-01' = {
  name: '${sqlserver.name}/AllowAllWindowsAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
}


// ------------------------------==[ WEB APP ]==------------------------------
resource webapp_hostingplan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  kind: 'windows'
}

resource webapp 'Microsoft.Web/sites@2020-06-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'app'
  properties: {
    serverFarmId: webapp_hostingplan.id
    httpsOnly: true
    enabled: true
    reserved: false
    hyperV: false
    isXenon: false
    siteConfig: {
      netFrameworkVersion: 'v5.0'
      scmType: 'None'
      managedPipelineMode: 'Integrated'
      virtualApplications: [
        {
          virtualPath: '/'
          physicalPath: 'site\\wwwroot'
          preloadEnabled: false
        }
      ]
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureAD__ClientId'
          value: aadAppId
        }
        {
          name: 'AzureAD__ClientSecret'
          value: aadAppSecret
        }
        {
          name: 'AzureAD__TenantId'
          value: subscription().tenantId
        }
        {
          name: 'AzureAD__Instance'
          value: environment().authentication.loginEndpoint
        }
        {
          name: 'AzureAD__CallbackPath'
          value: '/signin-oidc'
        }
        {
          name: 'Chimera__SignDecryptRole'
          value: signDecryptRole
        }
        {
          name: 'Chimera__VerifyEncryptRole'
          value: verifyEncryptRole
        }
        {
          name: 'Chimera__SubscriptionId'
          value: subscriptionId
        }
        {
          name: 'Chimera__AgentVaultName'
          value: agentkeyvault.name
        }
        {
          name: 'Chimera__UserVaultName'
          value: userkeyvault.name
        }
        {
          name: 'Chimera__ServiceBusNamespace'
          value: servicebus_namespace.name
        }
        {
          name: 'Chimera__PackageRepoAccount'
          value: storage_account.name
        }
        {
          name: 'Chimera__PackageRepoContainer'
          value: 'packages'
        }
        {
          name: 'Chimera__ResourceGroup'
          value: resourceGroupName
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: webapp_appinsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: webapp_appinsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Development'
        }
      ]
      connectionStrings: [
        {
          name: 'Database'
          connectionString: 'Data Source=tcp:${sqlserver.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};User Id=${sqlAdministratorLogin}@${sqlserver.properties.fullyQualifiedDomainName};Password=${sqlAdministratorLoginPassword};'
          type: 'SQLAzure'
        }
        {
          name: 'AzureWebJobsStorage'
          connectionString: 'DefaultEndpointsProtocol=https;AccountName=${storage_account.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storage_account.id, storage_account.apiVersion).keys[0].value}'
          type: 'Custom'
        }
        {
          name: 'AzureWebJobsDashboard'
          connectionString: 'DefaultEndpointsProtocol=https;AccountName=${storage_account.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storage_account.id, storage_account.apiVersion).keys[0].value}'
          type: 'Custom'
        }
      ]
    }
  }
}


// ----------------------------==[ APP INSIGHTS ]==----------------------------
resource webapp_appinsights 'Microsoft.Insights/components@2018-05-01-preview' = {
  name: 'AppInsights${webAppName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}


// -----------------------------==[ KEY VAULT - USERS ]==-----------------------------
resource userkeyvault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: userVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: vaultSku
    }
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
  }
}

// 00482a5a-887f-4fb3-b363-3b7fe8e74483 admin
// f25e0fa2-a7c8-4377-a976-54943a77a395 contrib
// 18d7d88d-d35e-4fb5-a5c3-7773c20a72d9 user access admin
resource userkeyvault_role1 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(userkeyvault.name, '00482a5a')
  scope: userkeyvault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    userkeyvault
  ]
}

resource userkeyvault_role2 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(userkeyvault.name, 'f25e0fa2')
  scope: userkeyvault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f25e0fa2-a7c8-4377-a976-54943a77a395')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    userkeyvault
  ]
}

resource userkeyvault_role3 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(userkeyvault.name, '18d7d88d')
  scope: userkeyvault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    userkeyvault
  ]
}

// ----------------------------==[ KEY VAULT - AGENTS ]==----------------------------
resource agentkeyvault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: agentVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: vaultSku
    }
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
  }
}

// 00482a5a-887f-4fb3-b363-3b7fe8e74483 admin
// f25e0fa2-a7c8-4377-a976-54943a77a395 contrib
// 18d7d88d-d35e-4fb5-a5c3-7773c20a72d9 user access admin
resource agentkeyvault_role1 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(agentkeyvault.name, '00482a5a')
  scope: agentkeyvault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    agentkeyvault
  ]
}

resource agentkeyvault_role2 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(agentkeyvault.name, 'f25e0fa2')
  scope: agentkeyvault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f25e0fa2-a7c8-4377-a976-54943a77a395')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    agentkeyvault
  ]
}

resource agentkeyvault_role3 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(agentkeyvault.name, '18d7d88d')
  scope: agentkeyvault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    agentkeyvault
  ]
}


// ----------------------------==[ SERVICE BUS ]==----------------------------
resource servicebus_namespace 'Microsoft.ServiceBus/namespaces@2017-04-01' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {}
}

resource serviceBus_identity 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(servicebus_namespace.name, '090c5cfd')
  scope: servicebus_namespace
  properties: {
    // service bus data owner
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    servicebus_namespace
  ]
}


// ------------------------------==[ STORAGE ]==------------------------------
resource storage_account 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

resource roleAssignment_debug 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = if (includeDebugRoles) {
  name: guid(storage_account.name, webapp.name, 'debug')
  scope: storage_account
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b') // Storage Blob Data Owner
    principalId: mainUserPrincipal
    principalType: 'User'
  }
  dependsOn: [
    webapp
  ]
}

resource roleAssignment_storage 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storage_account.name, webapp.name)
  scope: storage_account
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b') // Storage Blob Data Owner
    principalId: webapp.identity.principalId
    principalType: 'ServicePrincipal'
  }
  dependsOn: [
    webapp
  ]
}

resource pkgContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = {
  name: '${storage_account.name}/default/packages'
}
