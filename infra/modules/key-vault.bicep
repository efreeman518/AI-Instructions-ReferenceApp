@description('Resource name prefix')
param resourcePrefix string

@description('Azure region')
param location string

@description('Tags')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${resourcePrefix}-kv-${uniqueString(resourceGroup().id)}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7 // Short for dev
    enablePurgeProtection: true // Required for the Always Encrypted CMK (D-019). NOTE: irreversible once set.
    networkAcls: {
      defaultAction: 'Allow' // Dev: allow all; prod: restrict
      bypass: 'AzureServices'
    }
  }
}

// Column Master Key (CMK) for SQL Always Encrypted (D-019). RSA key; the migration signs its metadata and
// wraps the CEK with it. Consumers need the Key Vault Crypto User role (assigned in main.bicep).
resource cmkKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'taskflow-cmk'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'wrapKey'
      'unwrapKey'
      'sign'
      'verify'
    ]
  }
}

output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
output id string = keyVault.id
output cmkKeyUri string = cmkKey.properties.keyUriWithVersion
