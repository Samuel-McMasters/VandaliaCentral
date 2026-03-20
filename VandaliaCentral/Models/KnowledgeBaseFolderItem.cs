namespace VandaliaCentral.Models;

public class KnowledgeBaseFolderItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public List<KnowledgeBaseArticleItem> Articles { get; set; } = new();
    public List<Guid> SecurityTagIds { get; set; } = new();
    public List<string> AllowedGroupIds { get; set; } = new();
}
