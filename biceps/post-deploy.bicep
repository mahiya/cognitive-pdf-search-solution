//////////////////////////////////////////////////////////////////////
//// Parameters
//////////////////////////////////////////////////////////////////////

// Parameters for Existing Resources
param storageAccountName string
param blobContainerName string
param functionAppName string
param functionName string

// Parameters for New Resources
param location string = resourceGroup().location
param resourceNamePostfix string = uniqueString(resourceGroup().id)
param systemTopicName string = 'evgt-${resourceNamePostfix}'
param eventSubscriptionName string = 'evgs-${resourceNamePostfix}'

//////////////////////////////////////////////////////////////////////
//// Modules
//////////////////////////////////////////////////////////////////////

module eventGrid 'modules/event-grid.bicep' = {
  name: 'eventGrid'
  params: {
    storageAccountName: storageAccountName
    blobContainerName: blobContainerName
    functionAppName: functionAppName
    functionName: functionName
    location: location
    systemTopicName: systemTopicName
    eventSubscriptionName: eventSubscriptionName
    subjectEndsWiths: [ '.pdf' ]
  }
}
