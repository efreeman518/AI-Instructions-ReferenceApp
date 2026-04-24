@description('Resource name prefix')
param resourcePrefix string

@description('Azure region')
param location string

@description('Functions storage account name')
param funcStorageAccountName string

@description('Service Bus namespace FQDN')
param serviceBusNamespace string

@description('App Configuration endpoint')
param appConfigEndpoint string

@description('Key Vault URI')
param keyVaultUri string

@description('SQL connection string')
param sqlConnectionString string

@description('Cosmos DB endpoint')
param cosmosEndpoint string

@description('Storage blob endpoint')
param storageBlobEndpoint string

@description('Log Analytics workspace ID')
param logAnalyticsWorkspaceId string

@description('Tags')
param tags object = {}

var uniqueSuffix = uniqueString(resourceGroup().id)

resource funcStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: funcStorageAccountName
}

var funcStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${funcStorageAccount.name};AccountKey=${funcStorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

resource flexPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: '${resourcePrefix}-func-plan'
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true // Linux
  }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: '${resourcePrefix}-func-${uniqueSuffix}'
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: flexPlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        { name: 'AzureWebJobsStorage', value: funcStorageConnectionString }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'SERVICEBUS__fullyQualifiedNamespace', value: serviceBusNamespace }
        { name: 'AppConfig__Endpoint', value: appConfigEndpoint }
        { name: 'KeyVault__Uri', value: keyVaultUri }
        { name: 'ConnectionStrings__TaskFlowDbContextTrxn', value: sqlConnectionString }
        { name: 'ConnectionStrings__TaskFlowDbContextQuery', value: sqlConnectionString }
        { name: 'ConnectionStrings__CosmosDb1', value: cosmosEndpoint }
        { name: 'ConnectionStrings__BlobStorage1', value: storageBlobEndpoint }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-func-ai'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output functionAppName string = functionApp.name
output functionAppPrincipalId string = functionApp.identity.principalId
output functionAppHostName string = functionApp.properties.defaultHostName
