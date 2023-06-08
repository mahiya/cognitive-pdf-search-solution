using Microsoft.Extensions.Configuration;

namespace Functions
{
    public class FunctionConfiguration
    {
        public readonly string StorageAccountName;
        public readonly string DestinationStorageContainerName;

        public readonly string ComputerVisionRegion;
        public readonly string ComputerVisionKey;

        public readonly string CognitiveSearchName;
        public readonly string CognitiveSearchApiKey;
        public readonly string CognitiveSearchIndexerName;


        public FunctionConfiguration(IConfiguration config)
        {
            StorageAccountName = config["STORAGE_ACCOUNT_NAME"];
            DestinationStorageContainerName = config["DEST_STORAGE_CONTAINER_NAME"];

            ComputerVisionRegion = config["COMPUTER_VISION_REGION"];
            ComputerVisionKey = config["COMPUTER_VISION_KEY"];

            CognitiveSearchName = config["COGNITIVE_SEARCH_NAME"];
            CognitiveSearchApiKey = config["COGNITIVE_SEARCH_API_KEY"];
            CognitiveSearchIndexerName = config["COGNITIVE_SEARCH_INDEXER_NAME"];
        }
    }
}
