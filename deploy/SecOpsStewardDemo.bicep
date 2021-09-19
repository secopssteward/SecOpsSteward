@description('Random string to append to all resources in deployment')
param randomString string = uniqueString(resourceGroup().name)

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

@description('SecOps Steward Web App Name')
param webAppName string

var location = resourceGroup().location
var hostingPlanName = 'soshosting${randomString}'

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
          name: 'Chimera__ResourceGroup'
          value: resourceGroup().name
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
        {
          name: 'RunDemoMode'
          value: 'true'
        }
      ]
      connectionStrings: []
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
