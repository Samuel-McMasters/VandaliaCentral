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

        public async Task<FlappyLeaderboardSnapshot> GetLeaderboardsAsync(int top = 5)
        {
            var scores = await LoadScoresAsync();
            var now = DateTime.UtcNow;

            return new FlappyLeaderboardSnapshot
            {
                Daily = GetTopScoresForWindow(scores, GetDailyStartUtc(now), top),
                Weekly = GetTopScoresForWindow(scores, GetWeeklyStartUtc(now), top),
                Monthly = GetTopScoresForWindow(scores, GetMonthlyStartUtc(now), top),
                AllTime = GetTopScoresForWindow(scores, null, top)
            };
        }

        public async Task<List<FlappyScore>> GetTopScoresAsync(int top = 5)
        {
            var snapshot = await GetLeaderboardsAsync(top);
            return snapshot.AllTime;
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

        private static List<FlappyScore> GetTopScoresForWindow(IEnumerable<FlappyScore> scores, DateTime? windowStartUtc, int top)
        {
            var filteredScores = windowStartUtc.HasValue
                ? scores.Where(s => s.AchievedOnUtc >= windowStartUtc.Value)
                : scores;

            return filteredScores
                .GroupBy(s => (s.UserName ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(s => s.Score)
                    .ThenBy(s => s.AchievedOnUtc)
                    .First())
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.AchievedOnUtc)
                .Take(top)
                .ToList();
        }

        private static DateTime GetDailyStartUtc(DateTime nowUtc)
        {
            return nowUtc.Date;
        }

        private static DateTime GetWeeklyStartUtc(DateTime nowUtc)
        {
            var daysSinceMonday = ((int)nowUtc.DayOfWeek + 6) % 7;
            return nowUtc.Date.AddDays(-daysSinceMonday);
        }

        private static DateTime GetMonthlyStartUtc(DateTime nowUtc)
        {
            return new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 30, 0, DateTimeKind.Utc);
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
