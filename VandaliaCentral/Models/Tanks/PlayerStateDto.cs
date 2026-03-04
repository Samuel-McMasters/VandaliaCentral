namespace VandaliaCentral.Models.Tanks;

public sealed class PlayerStateDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float BodyAngle { get; set; }
    public float TurretAngle { get; set; }
    public int Hp { get; set; }
    public bool IsAlive { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
}
