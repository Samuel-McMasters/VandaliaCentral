namespace VandaliaCentral.Models;

public class KnowledgeBaseArticleItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
