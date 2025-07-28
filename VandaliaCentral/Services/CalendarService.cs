using Azure.Storage.Blobs;

using System.Text.Json;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class CalendarService
    {
        private readonly BlobContainerClient _containerClient;
        private const string BlobName = "company-calendar.json";

        public CalendarService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:connectionString"];
            _containerClient = new BlobContainerClient(connectionString, "calendar");
            _containerClient.CreateIfNotExists();
        }
        public async Task<List<CalendarEvent>> LoadCalendarAsync()
        {
            var blobClient = _containerClient.GetBlobClient(BlobName);

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                using var reader = new StreamReader(download.Value.Content.ToStream());
                var json = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var events = JsonSerializer.Deserialize<List<CalendarEvent>>(json) ?? new();
                    return events.OrderBy(e => e.Date).ToList();
                }
            }

            return new List<CalendarEvent>();
        }

        public async Task SaveCalendarAsync(List<CalendarEvent> events)
        {
            var json = JsonSerializer.Serialize(events.OrderBy(e => e.Date));
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            var blobClient = _containerClient.GetBlobClient(BlobName);
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
