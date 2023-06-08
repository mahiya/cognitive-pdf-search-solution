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
param systemTopicName string
param eventSubscriptionName string
param subjectEndsWiths array

//////////////////////////////////////////////////////////////////////
//// Definitions of New Resources
//////////////////////////////////////////////////////////////////////

// EventGrid: Topic
resource systemTopic 'Microsoft.EventGrid/systemTopics@2021-12-01' = {
  name: systemTopicName
  location: location
  properties: {
    source: resourceId('Microsoft.Storage/storageAccounts', storageAccountName)
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

// EventGrid: Subscription
resource eventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2021-12-01' = [for subjectEndsWith in subjectEndsWiths: {
  parent: systemTopic
  name: '${eventSubscriptionName}-${uniqueString(subjectEndsWith)}'
  properties: {
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
        resourceId: resourceId('Microsoft.Web/sites/functions', functionAppName, functionName)
      }
    }
    filter: {
      includedEventTypes: [ 'Microsoft.Storage.BlobCreated' ]
      subjectBeginsWith: '/blobServices/default/containers/${blobContainerName}'
      subjectEndsWith: subjectEndsWith
    }
  }
}]
