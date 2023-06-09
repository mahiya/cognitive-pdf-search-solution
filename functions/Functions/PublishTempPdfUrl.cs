using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Threading.Tasks;

namespace Functions.Functions
{
    /// <summary>
    /// アップロードされたPDFファイルに一時的にアクセスすることができるURLを発行するための Web API
    /// </summary>
    public class PublishTempPdfUrl
    {
        readonly BlobServiceClient _blobServiceClient;
        readonly string _containerName;

        public PublishTempPdfUrl(
            FunctionConfiguration config,
            BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = config.PdfsStorageContainerName;
        }

        [FunctionName(nameof(PublishTempPdfUrl))]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            // クエリストリングから一時アクセスURL発行の対象となるBlob名(ファイル名)を取得する
            // クエリストリングで指定していない場合はHttpステータスコード400(Bad Request)を返す
            const string queryKey = "n";
            if (!req.Query.ContainsKey(queryKey) || string.IsNullOrWhiteSpace(req.Query[queryKey]))
                return new BadRequestResult();
            var blobName = req.Query[queryKey];

            // 指定された Blob に一時的にアクセスすることができる URL を発行する
            var expiresOn = DateTime.UtcNow.AddMinutes(1); // 有効期限を1分とする
            var urlWithSas = await GetUrlWithSasAsync(blobName, BlobSasPermissions.Read, expiresOn);

            // 発行した一時アクセスURLを返す
            return new OkObjectResult(urlWithSas);
        }

        /// <summary>
        /// 指定した Blob に一時的にアクセスすることができる URL を発行する
        /// </summary>
        async Task<string> GetUrlWithSasAsync(string blobName, BlobSasPermissions permission, DateTime expiresOn)
        {
            var delegationKey = (await _blobServiceClient.GetUserDelegationKeyAsync(DateTime.UtcNow, expiresOn)).Value;
            var builder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTime.UtcNow,
                ExpiresOn = expiresOn
            };
            builder.SetPermissions(permission);
            var sasToken = builder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName);
            return $"{_blobServiceClient.Uri}{_containerName}/{blobName}?{sasToken}";
        }
    }
}
