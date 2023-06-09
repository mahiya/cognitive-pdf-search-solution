using Microsoft.Extensions.Configuration;

namespace Functions
{
    public class FunctionConfiguration
    {
        public readonly string StorageAccountName; // 使用する Azure Storage の名前
        public readonly string OcrResultsStorageContainerName; // PDFファイルの分析結果を格納するコンテナ名
        public readonly string PdfsStorageContainerName; // PDFファイルのアップロード先のコンテナ名

        public readonly string ComputerVisionRegion; // 使用する Azure Computer Vision のリージョン
        public readonly string ComputerVisionKey; // 使用する Azure Computer Vision のキー

        public readonly string CognitiveSearchName; // 使用する Azure Cognitive Search の名前
        public readonly string CognitiveSearchApiKey; // Azure Cognitive Search の管理者キー
        public readonly string CognitiveSearchIndexerName; // 起動するインデクサーの名前


        public FunctionConfiguration(IConfiguration config)
        {
            StorageAccountName = config["STORAGE_ACCOUNT_NAME"];
            OcrResultsStorageContainerName = config["STORAGE_CONTAINER_NAME_OCRRESULTS"];
            PdfsStorageContainerName = config["STORAGE_CONTAINER_NAME_PDFS"];

            ComputerVisionRegion = config["COMPUTER_VISION_REGION"];
            ComputerVisionKey = config["COMPUTER_VISION_KEY"];

            CognitiveSearchName = config["COGNITIVE_SEARCH_NAME"];
            CognitiveSearchApiKey = config["COGNITIVE_SEARCH_API_KEY"];
            CognitiveSearchIndexerName = config["COGNITIVE_SEARCH_INDEXER_NAME"];
        }
    }
}
