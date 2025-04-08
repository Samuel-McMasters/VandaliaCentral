using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Configuration;

namespace VandaliaCentral.Services
{
    public class PDFService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public PDFService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"];
            string containerName = "mondayminute";
            
            var blobServiceClient = new BlobServiceClient(connectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string?> GetLatestPdfUrlAsync()
        {
            BlobItem? latestBlob = null;
            DateTimeOffset? latestModified = null;

            await foreach (BlobItem blobItem in _blobContainerClient.GetBlobsAsync())
            {
                if (!blobItem.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (latestModified == null || blobItem.Properties.LastModified > latestModified)
                {
                    latestBlob = blobItem;
                    latestModified = blobItem.Properties.LastModified;
                }
            }

            if (latestBlob != null) 
            {
                var blobClient = _blobContainerClient.GetBlobClient(latestBlob.Name);
                return blobClient.Uri.ToString();
            }

            return null;


        }



    }
}
