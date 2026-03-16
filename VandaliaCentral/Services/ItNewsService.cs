using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class ItNewsService
    {
        private readonly BlobContainerClient _containerClient;
        private const string BlobName = "it-news.json";

        public ItNewsService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:connectionString"];
            _containerClient = new BlobContainerClient(connectionString, "itnews");
            _containerClient.CreateIfNotExists();
        }

        public async Task<List<ItNewsItem>> LoadNewsAsync()
        {
            var blobClient = _containerClient.GetBlobClient(BlobName);

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                using var reader = new StreamReader(download.Value.Content.ToStream());
                var json = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var items = JsonSerializer.Deserialize<List<ItNewsItem>>(json) ?? new List<ItNewsItem>();
                    var today = DateTime.Today;
                    var nonExpiredItems = items
                        .Where(i => !i.ExpirationDate.HasValue || i.ExpirationDate.Value.Date >= today)
                        .OrderByDescending(i => i.PostedOn)
                        .ToList();

                    if (nonExpiredItems.Count != items.Count)
                    {
                        await SaveNewsAsync(nonExpiredItems);
                    }

                    return nonExpiredItems;
                }
            }

            return new List<ItNewsItem>();
        }

        public async Task SaveNewsAsync(List<ItNewsItem> items)
        {
            var ordered = items
                .OrderByDescending(i => i.PostedOn)
                .ToList();

            var json = JsonSerializer.Serialize(ordered);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var blobClient = _containerClient.GetBlobClient(BlobName);
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
