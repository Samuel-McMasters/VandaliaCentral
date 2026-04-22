namespace VandaliaCentral.Models;

public class AdminTeamMemberLocation
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Mail { get; set; }
    public string? UserPrincipalName { get; set; }
    public string WorkLocation { get; set; } = "Unknown";
}
