using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;


namespace VandaliaCentral.Services
{
    public class PdfInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class PdfService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public PdfService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string?> GetLatestPdfUrlAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            BlobItem? latestBlob = null;
            DateTimeOffset? latestModified = null;

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                if (!blobItem.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (latestModified == null || blobItem.Properties.LastModified > latestModified)
                {
                    latestBlob = blobItem;
                    latestModified = blobItem.Properties.LastModified;
                }
            }

            if (latestBlob != null)
            {
                var blobClient = containerClient.GetBlobClient(latestBlob.Name);
                return blobClient.Uri.ToString();
            }

            return null;
        }


        public async Task<string?> GetMmPdfNameAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var pdfName = "";

            BlobItem? latestBlob = null;
            DateTimeOffset? latestModified = null;
            

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                if (!blobItem.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (latestModified == null || blobItem.Properties.LastModified > latestModified)
                {
                    latestBlob = blobItem;
                    latestModified = blobItem.Properties.LastModified;
                }
            }

            if (latestBlob != null)
            {

                var blobClient = containerClient.GetBlobClient(latestBlob.Name);
                pdfName = blobClient.Name.Substring(0, blobClient.Name.Length - 4);
                return pdfName;

            }

            return null;
        }
    }
}
