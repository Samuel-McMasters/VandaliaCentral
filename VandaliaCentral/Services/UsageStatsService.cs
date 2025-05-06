using Azure.Storage.Blobs;

public class UsageStatsService
{
    private readonly BlobContainerClient _containerClient;

    public UsageStatsService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        _containerClient = new BlobContainerClient(connectionString, "logs");
    }

    public async Task<Dictionary<string, int>> GetMonthlySidebarClicksAsync()
    {
        var clickCounts = new Dictionary<string, int>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync())
        {
            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
            var download = await blobClient.DownloadContentAsync();
            var content = download.Value.Content.ToString();

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("Clicked sidebar link"))
                {
                    // Extract date from line (assuming format: 2025-04-28 ...)
                    var datePart = line.Substring(0, 10); // YYYY-MM-DD
                    if (DateTime.TryParse(datePart, out var date))
                    {
                        var monthKey = date.ToString("yyyy-MM"); // e.g., "2025-04"
                        if (!clickCounts.ContainsKey(monthKey))
                            clickCounts[monthKey] = 0;

                        clickCounts[monthKey]++;
                    }
                }
            }
        }

        return clickCounts
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
