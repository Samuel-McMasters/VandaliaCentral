using System.Text.Json;

namespace VandaliaCentral.Game.Tanks;

public sealed class TanksMap
{
    public float Width { get; init; } = 1200;
    public float Height { get; init; } = 800;
    public List<RectObstacle> Obstacles { get; init; } = new();
    public List<SpawnPoint> SpawnPoints { get; init; } = new();

    public static TanksMap LoadOrDefault(string webRootPath)
    {
        var mapPath = Path.Combine(webRootPath, "maps", "map1.json");
        if (File.Exists(mapPath))
        {
            try
            {
                var json = File.ReadAllText(mapPath);
                var loaded = JsonSerializer.Deserialize<TanksMap>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loaded is not null && loaded.Width > 0 && loaded.Height > 0 && loaded.SpawnPoints.Count > 0)
                {
                    return loaded;
                }
            }
            catch
            {
                // Ignore malformed file and fallback to built-in map.
            }
        }

        return new TanksMap
        {
            Width = 1200,
            Height = 800,
            Obstacles =
            [
                new RectObstacle(300, 150, 120, 300),
                new RectObstacle(760, 120, 120, 260),
                new RectObstacle(520, 500, 180, 180),
                new RectObstacle(130, 560, 160, 120),
                new RectObstacle(940, 500, 180, 120)
            ],
            SpawnPoints =
            [
                new SpawnPoint(80, 80),
                new SpawnPoint(1120, 80),
                new SpawnPoint(80, 720),
                new SpawnPoint(1120, 720),
                new SpawnPoint(600, 80),
                new SpawnPoint(600, 720),
                new SpawnPoint(80, 400),
                new SpawnPoint(1120, 400)
            ]
        };
    }
}

public readonly record struct RectObstacle(float X, float Y, float Width, float Height);
public readonly record struct SpawnPoint(float X, float Y);
