targetScope = 'resourceGroup'

@description('Short lowercase prefix chosen for this optional isolated rehearsal.')
@minLength(3)
@maxLength(12)
param resourcePrefix string

@description('Azure region selected after checking service availability and current price.')
param location string = resourceGroup().location

@allowed([
  'app-service'
  'container-apps'
])
param hostingChoice string

@description('Pinned container reference supplied outside the repository when Container Apps is selected.')
param containerImageReference string

@description('Directory object identifier supplied outside the repository for the optional SQL Entra administrator.')
param deploymentOperatorObjectId string

@description('Display name supplied outside the repository for the optional SQL Entra administrator.')
param deploymentOperatorName string

var suffix = uniqueString(resourceGroup().id)
var compactName = take(replace('${resourcePrefix}${suffix}', '-', ''), 24)

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourcePrefix}-logs-${suffix}'
  location: location
  properties: {
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-insights-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    DisableLocalAuth: true
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: compactName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

resource vault 'Microsoft.KeyVault/vaults@2025-05-01' = {
  name: '${resourcePrefix}-kv-${suffix}'
  location: location
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource sql 'Microsoft.Sql/servers@2025-01-01' = {
  name: '${resourcePrefix}-sql-${suffix}'
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: deploymentOperatorName
      principalType: 'User'
      sid: deploymentOperatorObjectId
      tenantId: subscription().tenantId
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    version: '12.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sql
  name: 'app'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource appPlan 'Microsoft.Web/serverfarms@2024-11-01' = if (hostingChoice == 'app-service') {
  name: '${resourcePrefix}-plan-${suffix}'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2025-03-01' = if (hostingChoice == 'app-service') {
  name: '${resourcePrefix}-web-${suffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: appPlan.id
    siteConfig: {
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
    }
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' = if (hostingChoice == 'container-apps') {
  name: '${resourcePrefix}-env-${suffix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: listKeys(logs.id, logs.apiVersion).primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2025-01-01' = if (hostingChoice == 'container-apps') {
  name: '${resourcePrefix}-app-${suffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'app'
          image: containerImageReference
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        maxReplicas: 1
        minReplicas: 0
      }
    }
  }
}

output selectedHosting string = hostingChoice
output deletionScope string = resourceGroup().name
output nonSensitiveVaultUri string = vault.properties.vaultUri
