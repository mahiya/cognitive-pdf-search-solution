using Azure;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Functions.Startup))]

namespace Functions
{
    class Startup : FunctionsStartup
    {
        public IConfiguration Configuration { get; }

        public Startup()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("local.settings.json", true);
            Configuration = config.Build();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new FunctionConfiguration(Configuration);

            // FunctionConfiguration
            builder.Services.AddSingleton(provider => config);

            // BlobServiceClient
            builder.Services.AddSingleton(provider =>
            {
                var endpoint = new Uri($"https://{config.StorageAccountName}.blob.core.windows.net");
                var credential = new DefaultAzureCredential();
                return new BlobServiceClient(endpoint, credential);
            });

            // ComputerVisionClient
            builder.Services.AddSingleton(provider =>
            {
                var endpoint = $"https://{config.ComputerVisionRegion}.api.cognitive.microsoft.com/";
                var credential = new ApiKeyServiceClientCredentials(config.ComputerVisionKey);
                return new ComputerVisionClient(credential) { Endpoint = endpoint };
            });

            // SearchIndexerClient
            builder.Services.AddSingleton(provider =>
            {
                var endpoint = new Uri($"https://{config.CognitiveSearchName}.search.windows.net");
                var credential = new AzureKeyCredential(config.CognitiveSearchApiKey);
                return new SearchIndexerClient(endpoint, credential);
            });
        }
    }
}
