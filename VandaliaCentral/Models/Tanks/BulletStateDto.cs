namespace VandaliaCentral.Models.Tanks;

public sealed class BulletStateDto
{
    public int BulletId { get; set; }
    public string ShooterPlayerId { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Radius { get; set; }
}
