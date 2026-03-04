namespace VandaliaCentral.Models.Tanks;

public sealed class SnapshotDto
{
    public long Tick { get; set; }
    public float ArenaWidth { get; set; }
    public float ArenaHeight { get; set; }
    public List<PlayerStateDto> Players { get; set; } = new();
    public List<BulletStateDto> Bullets { get; set; } = new();
    public List<ObstacleDto> Obstacles { get; set; } = new();
}
