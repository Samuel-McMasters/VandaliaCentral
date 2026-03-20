namespace VandaliaCentral.Models;

public class KnowledgeBaseSecurityTagItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> AllowedGroupIds { get; set; } = new();
}
