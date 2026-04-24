@description('Resource name prefix')
param resourcePrefix string

@description('Azure region')
param location string

@description('Tags to apply')
param tags object = {}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourcePrefix}-log'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

output id string = workspace.id
output name string = workspace.name
output customerId string = workspace.properties.customerId
