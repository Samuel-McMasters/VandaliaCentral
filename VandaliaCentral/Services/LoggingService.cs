namespace VandaliaCentral.Services
{
    using Azure.Storage.Blobs;

    using System.Text;

    public class LoggingService
    {
        private readonly BlobContainerClient _containerClient;

        public LoggingService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _containerClient = new BlobContainerClient(connectionString, "userlogs");
            _containerClient.CreateIfNotExists();
        }

        public async Task LogActivityAsync(string userId, string activity)
        {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string blobName = $"{date}.log";

            var blobClient = _containerClient.GetBlobClient(blobName);

            string logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | {userId} | {activity}\n";

            // Download the existing log (if any), then append
            var sb = new StringBuilder();

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                sb.Append(download.Value.Content.ToString());
            }

            sb.Append(logEntry);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
