namespace VandaliaCentral.Models.Tanks;

public sealed class LobbyStateDto
{
    public int MaxPlayers { get; set; }
    public List<LobbyPlayerDto> Players { get; set; } = new();
}

public sealed class LobbyPlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Deaths { get; set; }
}
