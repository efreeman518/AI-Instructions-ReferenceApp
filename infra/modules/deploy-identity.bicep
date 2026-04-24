@description('User-assigned managed identity name')
param identityName string

@description('Azure region')
param location string

@description('Tags')
param tags object = {}

resource deployIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: identityName
  location: location
  tags: tags
}

output principalId string = deployIdentity.properties.principalId
output clientId string = deployIdentity.properties.clientId
output id string = deployIdentity.id
output name string = deployIdentity.name
