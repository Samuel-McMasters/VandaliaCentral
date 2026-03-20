namespace VandaliaCentral.Models;

public class KnowledgeBaseArticleItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public int ViewCount { get; set; }
    public List<Guid> SecurityTagIds { get; set; } = new();
    public List<string> AllowedGroupIds { get; set; } = new();
}
