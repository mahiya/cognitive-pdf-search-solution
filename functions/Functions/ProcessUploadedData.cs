using Azure.Messaging.EventGrid;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functions
{
    public partial class ProcessUploadedData
    {
        readonly BlobServiceClient _blobServiceClient;
        readonly BlobContainerClient _blobContainerClient;
        readonly ComputerVisionClient _computerVisionClient;
        readonly SearchIndexerClient _searchIndexerClient;
        readonly string _searchIndexerName;

        public ProcessUploadedData(
            FunctionConfiguration config,
            BlobServiceClient blobServiceClient,
            ComputerVisionClient computerVisionClient,
            SearchIndexerClient searchIndexerClient)
        {
            _blobServiceClient = blobServiceClient;
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(config.OcrResultsStorageContainerName);
            _computerVisionClient = computerVisionClient;
            _searchIndexerClient = searchIndexerClient;
            _searchIndexerName = config.CognitiveSearchIndexerName;
        }

        [FunctionName(nameof(ProcessUploadedData))]
        public async Task RunAsync([EventGridTrigger] EventGridEvent e, ILogger logger)
        {
            // 入力を取得する
            var data = e.Data.ToString();
            var blobEvent = JsonConvert.DeserializeObject<BlobEvent>(data);
            logger.LogInformation($"Inputed Data: {data}");

            // アップロードされた Blob 情報を取得する
            var url = new Uri(blobEvent.url);
            var storageAccountName = url.Host.Replace(".blob.core.windows.net", string.Empty);
            var containerName = url.LocalPath.Split("/")[1];
            var blobName = url.LocalPath.Replace($"/{containerName}/", "");

            // Computer Vision API が使用するための Blob の SAS + URL を生成する
            var delegationKey = (await _blobServiceClient.GetUserDelegationKeyAsync(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10))).Value;
            var builder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddMinutes(10),
            };
            builder.SetPermissions(BlobSasPermissions.Read);
            var sasToken = builder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName);
            var urlWithSas = $"{_blobServiceClient.Uri}{containerName}/{blobName}?{sasToken}";

            // Computer Vision に OCR 処理リクエストを送る
            var resp = await _computerVisionClient.ReadAsync(urlWithSas, language: "ja");
            var operationId = Path.GetFileName(resp.OperationLocation);
            logger.LogInformation($"Computer Vision Operation ID: {operationId}");

            // 処理が完了するまで待機
            ReadOperationResult result;
            while (true)
            {
                result = await _computerVisionClient.GetReadResultAsync(new Guid(operationId));
                logger.LogInformation($"Read Operation Status: {result.Status}");
                if (result.Status == OperationStatusCodes.Succeeded) break;
                if (result.Status == OperationStatusCodes.Failed)
                    throw new Exception("Read operation is failed.");
                await Task.Delay(500);
            }

            // OCR 結果を取得
            var parsedDocument = OcredPdfDocument.ParseAnalyzeResult(result.AnalyzeResult);
            var pages = parsedDocument.Pages.Select(p => new
            {
                storageAccountName,
                containerName,
                blobName,
                pageNumber = p.PageNumber,
                text = p.Text,
            });

            // OCR 結果を Blob Storage へアップロードする
            var blobClient = _blobContainerClient.GetBlobClient(Path.GetFileNameWithoutExtension(blobName) + ".json");
            var json = JsonConvert.SerializeObject(pages, Formatting.Indented);
            var bytes = Encoding.UTF8.GetBytes(json);
            var binaryData = new BinaryData(bytes);
            await blobClient.UploadAsync(binaryData, overwrite: true);
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "application/json" });

            // Cognitive Search のインデクサーを起動する
            await _searchIndexerClient.RunIndexerAsync(_searchIndexerName);
        }
    }
}