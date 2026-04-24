@description('Resource name prefix')
param resourcePrefix string

@description('Azure region')
param location string

@description('Tags')
param tags object = {}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-09-01-preview' = {
  name: '${resourcePrefix}-appconfig-${uniqueString(resourceGroup().id)}'
  location: location
  tags: tags
  sku: {
    name: 'free'
  }
  properties: {
    disableLocalAuth: true // Managed identity only
    softDeleteRetentionInDays: 1 // Dev: minimal
  }
}

output name string = appConfig.name
output endpoint string = appConfig.properties.endpoint
output id string = appConfig.id
