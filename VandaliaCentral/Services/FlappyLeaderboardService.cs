using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class FlappyLeaderboardService
    {
        private readonly BlobContainerClient _containerClient;
        private const string BlobName = "flappy-leaderboard.json";

        public FlappyLeaderboardService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:connectionString"];
            _containerClient = new BlobContainerClient(connectionString, "games");
            _containerClient.CreateIfNotExists();
        }

        public async Task<List<FlappyScore>> GetTopScoresAsync(int top = 5)
        {
            var scores = await LoadScoresAsync();

            return scores
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.AchievedOnUtc)
                .Take(top)
                .ToList();
        }


        public async Task ClearScoresAsync()
        {
            var blobClient = _containerClient.GetBlobClient(BlobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task AddScoreAsync(string userName, int score)
        {
            if (score <= 0)
            {
                return;
            }

            var scores = await LoadScoresAsync();
            scores.Add(new FlappyScore
            {
                UserName = string.IsNullOrWhiteSpace(userName) ? "anonymous" : userName,
                Score = score,
                AchievedOnUtc = DateTime.UtcNow
            });

            await SaveScoresAsync(scores);
        }

        private async Task<List<FlappyScore>> LoadScoresAsync()
        {
            var blobClient = _containerClient.GetBlobClient(BlobName);

            if (!await blobClient.ExistsAsync())
            {
                return new List<FlappyScore>();
            }

            var download = await blobClient.DownloadContentAsync();
            using var reader = new StreamReader(download.Value.Content.ToStream());
            var json = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<FlappyScore>();
            }

            return JsonSerializer.Deserialize<List<FlappyScore>>(json) ?? new List<FlappyScore>();
        }

        private async Task SaveScoresAsync(List<FlappyScore> scores)
        {
            var ordered = scores
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.AchievedOnUtc)
                .ToList();

            var json = JsonSerializer.Serialize(ordered);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var blobClient = _containerClient.GetBlobClient(BlobName);
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
