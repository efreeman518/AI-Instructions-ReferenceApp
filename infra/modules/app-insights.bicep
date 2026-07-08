@description('Resource name prefix')
param resourcePrefix string

@description('Azure region')
param location string

@description('Log Analytics workspace ID that backs this workspace-based Application Insights resource')
param logAnalyticsWorkspaceId string

@description('Tags to apply')
param tags object = {}

// Single shared, workspace-based Application Insights resource for every TaskFlow host
// (API, Gateway, Scheduler, Blazor, and Functions). Hosts export via the Azure Monitor
// OpenTelemetry distro in ServiceDefaults, gated on APPLICATIONINSIGHTS_CONNECTION_STRING,
// so they still run locally when that setting is absent.
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-ai'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output id string = appInsights.id
output name string = appInsights.name
output connectionString string = appInsights.properties.ConnectionString
output instrumentationKey string = appInsights.properties.InstrumentationKey
