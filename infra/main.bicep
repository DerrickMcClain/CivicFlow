// CivicFlow MVP hosting: Azure SQL + one Linux App Service plan running the API and the static frontend.
// Deploy into an existing resource group (rg-civicflow-mvp) with `az deployment group create`.

@description('Optional Entra tenant ID. Leave empty to keep seed JWT only.')
param azureAdTenantId string = ''

@description('Optional Entra API application (client) ID.')
param azureAdClientId string = ''

@description('Optional Entra API audience (App ID URI or client ID).')
param azureAdAudience string = ''

@description('Administrator login for the Azure SQL logical server.')
@minLength(4)
param sqlAdminLogin string

@description('Administrator password for the Azure SQL logical server.')
@secure()
@minLength(12)
param sqlAdminPassword string

@description('Signing key used by the API to sign JWTs. Must be at least 32 characters.')
@secure()
@minLength(32)
param jwtSigningKey string

@description('Azure region for every resource in this deployment.')
param location string = resourceGroup().location

var suffix = uniqueString(resourceGroup().id)
var appServicePlanName = 'plan-civicflow-${suffix}'
var apiAppName = 'app-civicflow-api-${suffix}'
var webAppName = 'app-civicflow-web-${suffix}'
var sqlServerName = 'sql-civicflow-${suffix}'
var sqlDatabaseName = 'CivicFlow'

// Built from the deterministic site name rather than webApp.properties.defaultHostName so the API
// can be given the frontend origin for CORS without creating a circular resource dependency.
var frontendOrigin = 'https://${webAppName}.azurewebsites.net'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// The 0.0.0.0 start/end pair is the "Allow Azure services and resources to access this server"
// rule, which is how the App Service outbound addresses reach the database.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;MultipleActiveResultSets=True'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          // App Service terminates TLS at the edge (httpsOnly above), so Kestrel listens on plain
          // HTTP. This also keeps the app's own HTTPS redirection off, avoiding a redirect loop.
          name: 'ASPNETCORE_URLS'
          value: 'http://+:8080'
        }
        {
          name: 'ConnectionStrings__CivicFlow'
          value: sqlConnectionString
        }
        {
          name: 'Jwt__Issuer'
          value: 'CivicFlow'
        }
        {
          name: 'Jwt__Audience'
          value: 'CivicFlow'
        }
        {
          name: 'Jwt__SigningKey'
          value: jwtSigningKey
        }
        {
          name: 'Jwt__ExpiryMinutes'
          value: '480'
        }
        {
          name: 'Cors__AllowedOrigin'
          value: frontendOrigin
        }
        {
          name: 'AzureAd__TenantId'
          value: azureAdTenantId
        }
        {
          name: 'AzureAd__ClientId'
          value: azureAdClientId
        }
        {
          name: 'AzureAd__Audience'
          value: azureAdAudience
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
      ]
    }
  }
  dependsOn: [
    sqlDatabase
    allowAzureServices
  ]
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|20-lts'
      // pm2 ships in the App Service Node images; --spa rewrites unknown paths to index.html so
      // React Router deep links resolve.
      appCommandLine: 'pm2 serve /home/site/wwwroot --no-daemon --spa'
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
      ]
    }
  }
}

output apiAppName string = apiApp.name
output webAppName string = webApp.name
output apiBaseUrl string = 'https://${apiApp.properties.defaultHostName}'
output webBaseUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
