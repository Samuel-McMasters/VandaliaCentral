namespace VandaliaCentral.Models.Tanks;

public sealed class InputDto
{
    public bool Forward { get; set; }
    public bool Backward { get; set; }
    public bool TurnLeft { get; set; }
    public bool TurnRight { get; set; }
    public bool FirePressed { get; set; }
    public float MouseX { get; set; }
    public float MouseY { get; set; }
}
