using Azure.Storage.Blobs;

using System.Text.Json;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class CalendarService
    {
        private readonly BlobContainerClient _containerClient;
        private const string BlobName = "company-calendar.json";
        private static readonly TimeZoneInfo EasternTimeZone = ResolveEasternTimeZone();

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
                    var activeEvents = RemoveExpiredEvents(events, out var removedAny);

                    if (removedAny)
                    {
                        await SaveCalendarAsync(activeEvents);
                    }

                    return activeEvents.OrderBy(e => e.Date).ToList();
                }
            }

            return new List<CalendarEvent>();
        }

        public async Task SaveCalendarAsync(List<CalendarEvent> events)
        {
            var activeEvents = RemoveExpiredEvents(events, out _);
            var json = JsonSerializer.Serialize(activeEvents.OrderBy(e => e.Date));
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            var blobClient = _containerClient.GetBlobClient(BlobName);
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        private static List<CalendarEvent> RemoveExpiredEvents(List<CalendarEvent> events, out bool removedAny)
        {
            var easternNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EasternTimeZone);
            var filtered = events
                .Where(e => e.Date.Date >= easternNow.Date)
                .ToList();

            removedAny = filtered.Count != events.Count;
            return filtered;
        }

        private static TimeZoneInfo ResolveEasternTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
        }
    }
}
