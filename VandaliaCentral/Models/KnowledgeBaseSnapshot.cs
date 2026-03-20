namespace VandaliaCentral.Models;

public class KnowledgeBaseSnapshot
{
    public List<KnowledgeBaseFolderItem> Folders { get; set; } = new();
    public List<KnowledgeBaseSecurityTagItem> SecurityTags { get; set; } = new();
}
