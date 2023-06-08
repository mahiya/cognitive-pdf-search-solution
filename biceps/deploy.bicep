//////////////////////////////////////////////////////////////////////
//// Parameters
//////////////////////////////////////////////////////////////////////

param location string = resourceGroup().location
param resourceNamePostfix string = uniqueString(resourceGroup().id)

param storageAccountName string = 'str${resourceNamePostfix}'
param storageContainerNames array = []

param cognitiveSearchName string = 'cogs-${resourceNamePostfix}'
param cognitiveSearchSku string = 'basic' // 'standard'
param cognitiveServiceName string = 'cog-${resourceNamePostfix}'
param computerVisionName string = 'cogv-${resourceNamePostfix}'

param functionAppName string = 'func-${resourceNamePostfix}'
param appInsightsName string = 'appi-${resourceNamePostfix}'
param appServicePlanName string = 'plan-${resourceNamePostfix}'

//////////////////////////////////////////////////////////////////////
//// Modules
//////////////////////////////////////////////////////////////////////

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    name: storageAccountName
    containerNames: storageContainerNames
  }
}

module cognitiveSearch 'modules/cognitive-search.bicep' = {
  name: 'cognitiveSearch'
  params: {
    location: location
    name: cognitiveSearchName
    sku: cognitiveSearchSku
  }
}

module cognitiveService 'modules/cognitive-service.bicep' = {
  name: 'cognitiveService'
  params: {
    location: location
    name: cognitiveServiceName
  }
}

module computerVision 'modules/computer-vision.bicep' = {
  name: 'computerVision'
  params: {
    location: location
    name: computerVisionName
  }
}

module functionApp 'modules/functions.bicep' = {
  name: 'functionApp'
  params: {
    location: location
    storageAccountName: storage.outputs.name
    functionAppName: functionAppName
    appInsightsName: appInsightsName
    appServicePlanName: appServicePlanName
  }
}

module functionRbac 'modules/rbac-blob-contributor.bicep' = {
  name: 'functionRbac'
  params: {
    storageAccountName: storage.outputs.name
    principalId: functionApp.outputs.principalId
  }
}

module cognitiveSearchRbac 'modules/rbac-blob-contributor.bicep' = {
  name: 'cognitiveSearchRbac'
  params: {
    storageAccountName: storage.outputs.name
    principalId: cognitiveSearch.outputs.principalId
  }
}

//////////////////////////////////////////////////////////////////////
//// Outputs
//////////////////////////////////////////////////////////////////////

output subscriptionId string = subscription().subscriptionId
output storageAccountName string = storage.outputs.name
output functionAppName string = functionApp.outputs.name
output cognitiveSearchName string = cognitiveSearch.outputs.name
output cognitiveServiceName string = cognitiveService.outputs.name
output computerVisionName string = computerVision.outputs.name
