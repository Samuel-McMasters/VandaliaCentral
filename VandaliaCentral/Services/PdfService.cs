using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components.Forms;


namespace VandaliaCentral.Services
{
    public class PdfInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset? LastModified { get; set; }
    }



    public class PdfService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public PdfService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            return _blobServiceClient.GetBlobContainerClient(containerName);
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
                var uriBuilder = new UriBuilder(blobClient.Uri);
                var cacheBustToken = latestModified?.ToUnixTimeMilliseconds().ToString() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                if (string.IsNullOrWhiteSpace(uriBuilder.Query))
                {
                    uriBuilder.Query = $"v={cacheBustToken}";
                }
                else
                {
                    uriBuilder.Query = $"{uriBuilder.Query.TrimStart('?')}&v={cacheBustToken}";
                }

                return uriBuilder.Uri.ToString();
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

        public async Task UploadPdfAsync(string containerName, IBrowserFile file)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Optional: Generate a unique file name with timestamp
            var fileName = $"{Path.GetFileNameWithoutExtension(file.Name)}.pdf";

            var blobClient = containerClient.GetBlobClient(fileName);

            // Upload file stream with content type
            var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "application/pdf" });
        }

        public async Task<List<PdfInfo>> GetPdfsAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var pdfs = new List<PdfInfo>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (!blobItem.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    continue;

                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var uriBuilder = new UriBuilder(blobClient.Uri);
                var cacheBustToken = blobItem.Properties.LastModified?.ToUnixTimeMilliseconds().ToString() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                if (string.IsNullOrWhiteSpace(uriBuilder.Query))
                {
                    uriBuilder.Query = $"v={cacheBustToken}";
                }
                else
                {
                    uriBuilder.Query = $"{uriBuilder.Query.TrimStart('?')}&v={cacheBustToken}";
                }

                pdfs.Add(new PdfInfo
                {
                    Name = Path.GetFileNameWithoutExtension(blobItem.Name),
                    Url = uriBuilder.Uri.ToString(),
                    LastModified = blobItem.Properties.LastModified
                });
            }

            return pdfs
                .OrderByDescending(pdf => pdf.LastModified ?? DateTimeOffset.MinValue)
                .ToList();
        }

        public async Task<List<BlobItem>> ListBlobsAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = new List<BlobItem>();

            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }

            return blobs;
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.DeleteBlobIfExistsAsync(blobName);
        }
    }
}
