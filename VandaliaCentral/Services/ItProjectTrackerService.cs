using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class ItProjectTrackerService
    {
        private readonly BlobContainerClient _containerClient;
        private const string UserBlobPrefix = "users";

        public ItProjectTrackerService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:connectionString"];
            _containerClient = new BlobContainerClient(connectionString, "itprojecttracker");
            _containerClient.CreateIfNotExists();
        }

        public async Task<List<ItProjectTrackerItem>> LoadItemsAsync(string userKey)
        {
            var blobClient = _containerClient.GetBlobClient(GetBlobName(userKey));

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                using var reader = new StreamReader(download.Value.Content.ToStream());
                var json = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var items = JsonSerializer.Deserialize<List<ItProjectTrackerItem>>(json) ?? new List<ItProjectTrackerItem>();
                    return items
                        .OrderByDescending(x => x.CompletedDate ?? DateTime.MinValue)
                        .ThenByDescending(x => x.LastUpdatedOn)
                        .ToList();
                }
            }

            return new List<ItProjectTrackerItem>();
        }

        public async Task SaveItemsAsync(string userKey, List<ItProjectTrackerItem> items)
        {
            var orderedItems = items
                .OrderByDescending(x => x.CompletedDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.LastUpdatedOn)
                .ToList();

            var json = JsonSerializer.Serialize(orderedItems);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var blobClient = _containerClient.GetBlobClient(GetBlobName(userKey));
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        private static string GetBlobName(string? userKey)
        {
            var normalizedUserKey = string.IsNullOrWhiteSpace(userKey)
                ? "anonymous"
                : Uri.EscapeDataString(userKey.Trim().ToLowerInvariant());

            return $"{UserBlobPrefix}/{normalizedUserKey}.json";
        }
    }
}
