@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Name of the Web App')
param webAppName string

@description('SKU for App Service Plan (e.g., B1, S1, P1v3)')
param skuName string = 'B1'

@description('Linux runtime stack for the Web App')
param linuxFxVersion string = 'DOTNET|9.0'

@description('Application Insights resource name')
param appInsightsName string = '${webAppName}-ai'

@allowed([
  'web'
  'other'
])
@description('Application Insights application type')
param appInsightsType string = 'web'

var planTier = skuName == 'B1' ? 'Basic' : (skuName == 'S1' ? 'Standard' : (contains(skuName, 'P') ? 'PremiumV3' : 'Basic'))

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
    tier: planTier
    capacity: 1
  }
  properties: {
    reserved: true // Linux
  }
  kind: 'app,linux'
}

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      appCommandLine: ''
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

resource ai 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: appInsightsType
    Flow_Type: 'Bluefield'
    Request_Source: 'AzureTemplate'
  }
}

resource appSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  name: 'appsettings'
  parent: site
  properties: {
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai.properties.ConnectionString
    APPINSIGHTS_INSTRUMENTATIONKEY: ai.properties.InstrumentationKey
    ASPNETCORE_ENVIRONMENT: 'Production'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    SCM_DO_BUILD_DURING_DEPLOYMENT: 'false'
  }
}

output webAppNameOut string = site.name
output webAppUrl string = 'https://${site.name}.azurewebsites.net'
output appServicePlanId string = plan.id
output appInsightsConnectionString string = ai.properties.ConnectionString
