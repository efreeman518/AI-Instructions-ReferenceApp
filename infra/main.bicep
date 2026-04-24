// ============================================================================
// TaskFlow Dev Environment — Main Bicep
// Single resource group, minimal SKUs, managed identities, Entra-only auth
// ============================================================================

targetScope = 'subscription'

// ---- Parameters ----

@description('Resource name prefix')
param resourcePrefix string = 'taskflow'

@description('Environment name')
param environmentName string = 'dev'

@description('Azure region')
param location string = 'eastus2'

@description('SQL Entra admin principal ID')
param sqlAdminPrincipalId string

@description('SQL Entra admin principal name')
param sqlAdminPrincipalName string

@description('SQL Entra admin principal type')
@allowed(['User', 'Group', 'Application'])
param sqlAdminPrincipalType string = 'User'

@description('Gateway container image')
param gatewayImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('API container image')
param apiImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Scheduler container image')
param schedulerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Blazor container image')
param blazorImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

// ---- Variables ----

var prefix = '${resourcePrefix}-${environmentName}'
var tags = {
  environment: environmentName
  project: 'taskflow'
  managedBy: 'bicep'
}

// Well-known RBAC role definition IDs
var roles = {
  // Storage
  storageBlobDataContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  // Cosmos DB
  cosmosDbDataContributor: '00000000-0000-0000-0000-000000000002' // Built-in Cosmos data role
  // Service Bus
  serviceBusDataSender: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
  serviceBusDataReceiver: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
  // Key Vault
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  // App Configuration
  appConfigDataReader: '516239f1-63e1-4d78-a4de-a74fb236a071'
  // Contributor (for deploy identity)
  contributor: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
  userAccessAdministrator: '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9'
}

// ---- Resource Group ----

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: '${prefix}-rg'
  location: location
  tags: tags
}

// ---- Foundation Modules ----

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalytics'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    tags: tags
  }
}

module containerAppsEnv 'modules/container-apps-environment.bicep' = {
  name: 'containerAppsEnv'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    tags: tags
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    tags: tags
  }
}

module appConfig 'modules/app-configuration.bicep' = {
  name: 'appConfig'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    tags: tags
  }
}

// ---- Data Modules ----

module sqlDatabase 'modules/sql-database.bicep' = {
  name: 'sqlDatabase'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    sqlAdminPrincipalId: sqlAdminPrincipalId
    sqlAdminPrincipalName: sqlAdminPrincipalName
    sqlAdminPrincipalType: sqlAdminPrincipalType
    tags: tags
  }
}

module cosmosDb 'modules/cosmos-db.bicep' = {
  name: 'cosmosDb'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    tags: tags
  }
}

module serviceBus 'modules/service-bus.bicep' = {
  name: 'serviceBus'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    tags: tags
  }
}

// ---- Compute: Container Apps ----

var commonEnvVars = [
  { name: 'AppConfig__Endpoint', value: appConfig.outputs.endpoint }
  { name: 'KeyVault__Uri', value: keyVault.outputs.uri }
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
]

module gateway 'modules/container-app.bicep' = {
  name: 'gateway'
  scope: rg
  params: {
    appName: '${prefix}-gateway'
    location: location
    environmentId: containerAppsEnv.outputs.id
    containerImage: gatewayImage
    cpu: '0.25'
    memory: '0.5Gi'
    externalIngress: true // Only public endpoint
    targetPort: 8080
    minReplicas: 0
    maxReplicas: 2
    envVars: union(commonEnvVars, [
      { name: 'ReverseProxy__Clusters__api__Destinations__default__Address', value: 'https://${api.outputs.fqdn}' }
    ])
    tags: tags
  }
}

module api 'modules/container-app.bicep' = {
  name: 'api'
  scope: rg
  params: {
    appName: '${prefix}-api'
    location: location
    environmentId: containerAppsEnv.outputs.id
    containerImage: apiImage
    cpu: '0.5'
    memory: '1Gi'
    externalIngress: false // Internal only
    targetPort: 8080
    minReplicas: 0
    maxReplicas: 3
    envVars: union(commonEnvVars, [
      { name: 'ConnectionStrings__TaskFlowDbContextTrxn', value: sqlDatabase.outputs.connectionString }
      { name: 'ConnectionStrings__TaskFlowDbContextQuery', value: sqlDatabase.outputs.connectionString }
      { name: 'ConnectionStrings__CosmosDb1', value: cosmosDb.outputs.accountEndpoint }
      { name: 'ConnectionStrings__BlobStorage1', value: storage.outputs.appStorageBlobEndpoint }
      { name: 'ConnectionStrings__TableStorage1', value: storage.outputs.appStorageTableEndpoint }
      { name: 'SERVICEBUS__fullyQualifiedNamespace', value: serviceBus.outputs.namespaceEndpoint }
    ])
    tags: tags
  }
}

module scheduler 'modules/container-app.bicep' = {
  name: 'scheduler'
  scope: rg
  params: {
    appName: '${prefix}-scheduler'
    location: location
    environmentId: containerAppsEnv.outputs.id
    containerImage: schedulerImage
    cpu: '0.25'
    memory: '0.5Gi'
    ingressEnabled: false // No ingress needed
    targetPort: 8080
    minReplicas: 0
    maxReplicas: 1
    envVars: union(commonEnvVars, [
      { name: 'ConnectionStrings__TaskFlowDbContextQuery', value: sqlDatabase.outputs.connectionString }
      { name: 'SERVICEBUS__fullyQualifiedNamespace', value: serviceBus.outputs.namespaceEndpoint }
    ])
    tags: tags
  }
}

module blazor 'modules/container-app.bicep' = {
  name: 'blazor'
  scope: rg
  params: {
    appName: '${prefix}-blazor'
    location: location
    environmentId: containerAppsEnv.outputs.id
    containerImage: blazorImage
    cpu: '0.25'
    memory: '0.5Gi'
    externalIngress: true
    targetPort: 8080
    minReplicas: 0
    maxReplicas: 1
    envVars: [
      { name: 'ApiBaseUrl', value: 'https://${gateway.outputs.fqdn}' }
      { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
    ]
    tags: tags
  }
}

module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'staticWebApp'
  scope: rg
  params: {
    appName: '${prefix}-uno'
    location: location
    tags: tags
  }
}

// ---- Compute: Functions ----

module functions 'modules/functions.bicep' = {
  name: 'functions'
  scope: rg
  params: {
    resourcePrefix: prefix
    location: location
    funcStorageAccountName: storage.outputs.funcStorageName
    serviceBusNamespace: serviceBus.outputs.namespaceEndpoint
    appConfigEndpoint: appConfig.outputs.endpoint
    keyVaultUri: keyVault.outputs.uri
    sqlConnectionString: sqlDatabase.outputs.connectionString
    cosmosEndpoint: cosmosDb.outputs.accountEndpoint
    storageBlobEndpoint: storage.outputs.appStorageBlobEndpoint
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    tags: tags
  }
}

// ---- Deploy Identity ----

module deployIdentity 'modules/deploy-identity.bicep' = {
  name: 'deployIdentity'
  scope: rg
  params: {
    identityName: '${prefix}-deploy-id'
    location: location
    tags: tags
  }
}

// ---- RBAC: Deploy Identity → Resource Group ----

module deployContributor 'modules/role-assignment.bicep' = {
  name: 'deployContributor'
  scope: rg
  params: {
    principalId: deployIdentity.outputs.principalId
    roleDefinitionId: roles.contributor
    roleDescription: 'Deploy identity: Contributor on RG'
  }
}

module deployUaa 'modules/role-assignment.bicep' = {
  name: 'deployUaa'
  scope: rg
  params: {
    principalId: deployIdentity.outputs.principalId
    roleDefinitionId: roles.userAccessAdministrator
    roleDescription: 'Deploy identity: User Access Administrator on RG'
  }
}

// ---- RBAC: Gateway ----

module gatewayAppConfigReader 'modules/role-assignment.bicep' = {
  name: 'gatewayAppConfigReader'
  scope: rg
  params: {
    principalId: gateway.outputs.principalId
    roleDefinitionId: roles.appConfigDataReader
    roleDescription: 'Gateway: App Configuration Data Reader'
  }
}

module gatewayKvSecretsUser 'modules/role-assignment.bicep' = {
  name: 'gatewayKvSecretsUser'
  scope: rg
  params: {
    principalId: gateway.outputs.principalId
    roleDefinitionId: roles.keyVaultSecretsUser
    roleDescription: 'Gateway: Key Vault Secrets User'
  }
}

// ---- RBAC: API ----

module apiAppConfigReader 'modules/role-assignment.bicep' = {
  name: 'apiAppConfigReader'
  scope: rg
  params: {
    principalId: api.outputs.principalId
    roleDefinitionId: roles.appConfigDataReader
    roleDescription: 'API: App Configuration Data Reader'
  }
}

module apiKvSecretsUser 'modules/role-assignment.bicep' = {
  name: 'apiKvSecretsUser'
  scope: rg
  params: {
    principalId: api.outputs.principalId
    roleDefinitionId: roles.keyVaultSecretsUser
    roleDescription: 'API: Key Vault Secrets User'
  }
}

module apiServiceBusSender 'modules/role-assignment.bicep' = {
  name: 'apiServiceBusSender'
  scope: rg
  params: {
    principalId: api.outputs.principalId
    roleDefinitionId: roles.serviceBusDataSender
    roleDescription: 'API: Service Bus Data Sender'
  }
}

module apiBlobContributor 'modules/role-assignment.bicep' = {
  name: 'apiBlobContributor'
  scope: rg
  params: {
    principalId: api.outputs.principalId
    roleDefinitionId: roles.storageBlobDataContributor
    roleDescription: 'API: Storage Blob Data Contributor'
  }
}

module apiTableContributor 'modules/role-assignment.bicep' = {
  name: 'apiTableContributor'
  scope: rg
  params: {
    principalId: api.outputs.principalId
    roleDefinitionId: roles.storageTableDataContributor
    roleDescription: 'API: Storage Table Data Contributor'
  }
}

// ---- RBAC: Scheduler ----

module schedulerAppConfigReader 'modules/role-assignment.bicep' = {
  name: 'schedulerAppConfigReader'
  scope: rg
  params: {
    principalId: scheduler.outputs.principalId
    roleDefinitionId: roles.appConfigDataReader
    roleDescription: 'Scheduler: App Configuration Data Reader'
  }
}

module schedulerKvSecretsUser 'modules/role-assignment.bicep' = {
  name: 'schedulerKvSecretsUser'
  scope: rg
  params: {
    principalId: scheduler.outputs.principalId
    roleDefinitionId: roles.keyVaultSecretsUser
    roleDescription: 'Scheduler: Key Vault Secrets User'
  }
}

module schedulerServiceBusSender 'modules/role-assignment.bicep' = {
  name: 'schedulerServiceBusSender'
  scope: rg
  params: {
    principalId: scheduler.outputs.principalId
    roleDefinitionId: roles.serviceBusDataSender
    roleDescription: 'Scheduler: Service Bus Data Sender'
  }
}

// ---- RBAC: Functions ----

module funcServiceBusReceiver 'modules/role-assignment.bicep' = {
  name: 'funcServiceBusReceiver'
  scope: rg
  params: {
    principalId: functions.outputs.functionAppPrincipalId
    roleDefinitionId: roles.serviceBusDataReceiver
    roleDescription: 'Functions: Service Bus Data Receiver'
  }
}

module funcBlobContributor 'modules/role-assignment.bicep' = {
  name: 'funcBlobContributor'
  scope: rg
  params: {
    principalId: functions.outputs.functionAppPrincipalId
    roleDefinitionId: roles.storageBlobDataContributor
    roleDescription: 'Functions: Storage Blob Data Contributor'
  }
}

module funcAppConfigReader 'modules/role-assignment.bicep' = {
  name: 'funcAppConfigReader'
  scope: rg
  params: {
    principalId: functions.outputs.functionAppPrincipalId
    roleDefinitionId: roles.appConfigDataReader
    roleDescription: 'Functions: App Configuration Data Reader'
  }
}

module funcKvSecretsUser 'modules/role-assignment.bicep' = {
  name: 'funcKvSecretsUser'
  scope: rg
  params: {
    principalId: functions.outputs.functionAppPrincipalId
    roleDefinitionId: roles.keyVaultSecretsUser
    roleDescription: 'Functions: Key Vault Secrets User'
  }
}

// ---- Cosmos DB RBAC (data plane — separate module for RG scope) ----

module cosmosRbac 'modules/cosmos-rbac.bicep' = {
  name: 'cosmosRbac'
  scope: rg
  params: {
    cosmosAccountName: cosmosDb.outputs.accountName
    principalIds: [
      api.outputs.principalId
      functions.outputs.functionAppPrincipalId
    ]
  }
}

// ---- Outputs ----

output resourceGroupName string = rg.name
output gatewayFqdn string = gateway.outputs.fqdn
output apiFqdn string = api.outputs.fqdn
output blazorFqdn string = blazor.outputs.fqdn
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppDefaultHostname string = staticWebApp.outputs.defaultHostname
output functionAppName string = functions.outputs.functionAppName
output deployIdentityClientId string = deployIdentity.outputs.clientId
output deployIdentityPrincipalId string = deployIdentity.outputs.principalId
output keyVaultName string = keyVault.outputs.name
output appConfigName string = appConfig.outputs.name
output sqlServerName string = sqlDatabase.outputs.serverName
