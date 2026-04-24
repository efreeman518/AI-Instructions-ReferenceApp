@description('Resource name prefix')
param resourcePrefix string

@description('Azure region')
param location string

@description('Tags')
param tags object = {}

var uniqueSuffix = uniqueString(resourceGroup().id)

// App data storage (blob + tables)
resource appStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: replace('${resourcePrefix}st${uniqueSuffix}', '-', '')
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // Entra-only auth
    defaultToOAuthAuthentication: true
    networkAcls: {
      defaultAction: 'Allow' // Dev: allow all; prod: restrict
      bypass: 'AzureServices'
    }
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: appStorage
  name: 'default'
}

resource attachmentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'attachments'
  properties: {
    publicAccess: 'None'
  }
}

resource tableServices 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: appStorage
  name: 'default'
}

// Functions runtime storage (requires shared key for Functions runtime)
resource funcStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: replace('${resourcePrefix}fn${uniqueSuffix}', '-', '')
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Required for Functions runtime
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

output appStorageName string = appStorage.name
output appStorageBlobEndpoint string = appStorage.properties.primaryEndpoints.blob
output appStorageTableEndpoint string = appStorage.properties.primaryEndpoints.table
output funcStorageName string = funcStorage.name
output funcStorageId string = funcStorage.id
