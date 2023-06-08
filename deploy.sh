#!/bin/bash -e

# 変数を定義する
region='japaneast'    # デプロイ先のリージョン
resourceGroupName=$1  # デプロイ先のリソースグループ (スクリプトの引数から取得する)
pdfsBlobContainerName='pdfs'
ocrResultsBlobContainerName='ocrresults'

# Cognitive Search に関する名前を定義する
cognitiveSearchIndexName="pdf-search"
cognitiveSearchDataSourceName="pdf-search"
cognitiveSearchSkillSetName="pdf-search"
cognitiveSearchIndexerName="pdf-search"

# リソースグループを作成する
az group create \
    --location $region \
    --resource-group $resourceGroupName

# Azure リソースをデプロイする
outputs=($(az deployment group create \
            --resource-group $resourceGroupName \
            --template-file biceps/deploy.bicep \
            --parameters storageContainerNames=[\"$pdfsBlobContainerName\",\"$ocrResultsBlobContainerName\"] \
            --query 'properties.outputs.*.value' \
            --output tsv))
subscriptionId=`echo ${outputs[0]}` # 文末の \r を削除する
storageAccountName=`echo ${outputs[1]}` # 文末の \r を削除する
functionAppName=`echo ${outputs[2]}` # 文末の \r を削除する
cognitiveSearchName=`echo ${outputs[3]}` # 文末の \r を削除する
cognitiveServiceName=`echo ${outputs[4]}` # 文末の \r を削除する
computerVisionName=${outputs[5]}

# Cognitive Service の API キーを取得する
computerVisionKey=`az cognitiveservices account keys list --name $computerVisionName --resource-group $resourceGroupName --query 'key1' --output tsv`

# Cognitive Search の API キーを取得する
cognitiveSearchApiKey=`az search admin-key show --service-name $cognitiveSearchName --resource-group $resourceGroupName --query 'primaryKey' --output tsv`

# Azure Functions のアプリケーション設定を設定する
az functionapp config appsettings set \
    --resource-group $resourceGroupName \
    --name $functionAppName \
    --settings "STORAGE_ACCOUNT_NAME=$storageAccountName" \
               "DEST_STORAGE_CONTAINER_NAME=$ocrResultsBlobContainerName" \
               "COMPUTER_VISION_REGION=$region" \
               "COMPUTER_VISION_KEY=$computerVisionKey" \
               "COGNITIVE_SEARCH_NAME=$cognitiveSearchName" \
               "COGNITIVE_SEARCH_API_KEY=$cognitiveSearchApiKey" \
               "COGNITIVE_SEARCH_INDEXER_NAME=$cognitiveSearchIndexerName"

# Azure Functions のアプリケーションをデプロイする
pushd functions
sleep 10 # Azure Functions App リソースの作成からコードデプロイが早すぎると「リソースが見つからない」エラーが発生する場合があるので、一時停止する
func azure functionapp publish $functionAppName --csharp
popd

# EventGrid をデプロイする
az deployment group create \
    --resource-group $resourceGroupName \
    --template-file biceps/post-deploy.bicep \
    --parameters storageAccountName=$storageAccountName \
                 blobContainerName=$blobContainerName \
                 functionAppName=$functionAppName \
                 functionName='ProcessUploadedData'

# 使用する Cognitive Search REST API のバージョンを指定する
cognitiveSearchApiVersion='2020-06-30'

# Cognitive Service の API キーを取得する
cognitiveServiceKey=`az cognitiveservices account keys list --name $cognitiveServiceName --resource-group $resourceGroupName --query 'key1' --output tsv`

# Cognitive Search インデックスを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/indexes/$cognitiveSearchIndexName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d @cogsearch/index.json

# Cognitive Search データソースを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/datasources/$cognitiveSearchDataSourceName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d "$(sed -e "s|{{CONNECTION_STRING}}|ResourceId=/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Storage/storageAccounts/$storageAccountName;|; \
                  s|{{CONTAINER_NAME}}|$ocrResultsBlobContainerName|;" \
                  "cogsearch/datasource.json")"

# Cognitive Search スキルセットを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/skillsets/$cognitiveSearchSkillSetName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d "$(sed -e "s|{{COGNITIVE_SERVICE_KEY}}|$cognitiveServiceKey|;" \
                  "cogsearch/skillset.json")"

# Cognitive Search インデクサーを作成する
curl -X PUT https://$cognitiveSearchName.search.windows.net/indexers/$cognitiveSearchIndexerName?api-version=$cognitiveSearchApiVersion \
    -H 'Content-Type: application/json' \
    -H 'api-key: '$cognitiveSearchApiKey \
    -d "$(sed -e "s|{{DATASOURCE_NAME}}|$cognitiveSearchDataSourceName|; \
                  s|{{SKILLSET_NAME}}|$cognitiveSearchSkillSetName|; \
                  s|{{INDEX_NAME}}|$cognitiveSearchIndexName|;" \
                  "cogsearch/indexer.json")"

# Cognitive Search へアクセスするためのクエリキーを取得する
cognitiveSearchQueryKey=`az search query-key list --resource-group $resourceGroupName --service-name $cognitiveSearchName --query "[0].key" --output tsv`

# テンプレートから "app/settings.js" ファイルを作成する
sed -e "s/{{NAME}}/$cognitiveSearchName/g" \
    -e "s/{{KEY}}/$cognitiveSearchQueryKey/g" \
    -e "s/{{INDEX_NAME}}/$cognitiveSearchIndexName/g" \
    "app/settings_template.js" > app/settings.js
