using 'main.bicep'

param resourcePrefix = 'taskflow'
param environmentName = 'dev'
param location = 'eastus2'

// SQL Entra admin — set these before deployment:
// az ad signed-in-user show --query "{id:id, name:displayName}" -o json
param sqlAdminPrincipalId = '' // Your Entra user/group object ID
param sqlAdminPrincipalName = '' // Your Entra user/group display name
param sqlAdminPrincipalType = 'User'

// Container images — updated by CI/CD workflow
param gatewayImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param apiImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param schedulerImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param blazorImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
