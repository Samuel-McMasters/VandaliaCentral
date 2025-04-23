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

            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);

            string logEntry = $"{easternTime:yyyy-MM-dd HH:mm:ss} EST | {userId} | {activity}\n";

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

        public async Task<List<string>> GetLogFileNamesAsync()
        {
            var files = new List<string>();

            await foreach (var blob in _containerClient.GetBlobsAsync())
            {
                files.Add(blob.Name);
            }

            return files.OrderByDescending(name => name).ToList();
        }

        public async Task<string> ReadLogContentAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                return download.Value.Content.ToString();
            }

            return "Log file not found.";
        }
    }
}
