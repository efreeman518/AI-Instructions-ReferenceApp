param cosmosAccountName string
param principalIds array

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

// Built-in "Cosmos DB Built-in Data Contributor" role
var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002'

resource cosmosRoleAssignments 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = [
  for (principalId, i) in principalIds: {
    name: guid(cosmosAccount.id, principalId, 'cosmos-data-contributor')
    parent: cosmosAccount
    properties: {
      roleDefinitionId: '${cosmosAccount.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
      principalId: principalId
      scope: cosmosAccount.id
    }
  }
]
