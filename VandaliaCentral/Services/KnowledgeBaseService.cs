using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public class KnowledgeBaseService
{
    private readonly BlobContainerClient _containerClient;
    private const string BlobName = "knowledge-base.json";

    public KnowledgeBaseService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:connectionString"];
        _containerClient = new BlobContainerClient(connectionString, "knowledgebase");
        _containerClient.CreateIfNotExists();
    }

    public async Task<KnowledgeBaseSnapshot> LoadSnapshotAsync()
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);

        if (!await blobClient.ExistsAsync())
        {
            return new KnowledgeBaseSnapshot();
        }

        var download = await blobClient.DownloadContentAsync();
        using var reader = new StreamReader(download.Value.Content.ToStream());
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return new KnowledgeBaseSnapshot();
        }

        // Backwards compatibility: old payload is just a JSON array of folders.
        if (json.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            var legacyFolders = JsonSerializer.Deserialize<List<KnowledgeBaseFolderItem>>(json) ?? new List<KnowledgeBaseFolderItem>();
            NormalizeFolders(legacyFolders);
            return new KnowledgeBaseSnapshot
            {
                Folders = legacyFolders
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                SecurityTags = new List<KnowledgeBaseSecurityTagItem>()
            };
        }

        var snapshot = JsonSerializer.Deserialize<KnowledgeBaseSnapshot>(json) ?? new KnowledgeBaseSnapshot();

        snapshot.Folders ??= new List<KnowledgeBaseFolderItem>();
        snapshot.SecurityTags ??= new List<KnowledgeBaseSecurityTagItem>();

        NormalizeFolders(snapshot.Folders);

        foreach (var securityTag in snapshot.SecurityTags)
        {
            securityTag.AllowedGroupIds = NormalizeGroupIds(securityTag.AllowedGroupIds);
        }

        snapshot.Folders = snapshot.Folders
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        snapshot.SecurityTags = snapshot.SecurityTags
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return snapshot;
    }

    public async Task SaveSnapshotAsync(KnowledgeBaseSnapshot snapshot)
    {
        snapshot.Folders ??= new List<KnowledgeBaseFolderItem>();
        snapshot.SecurityTags ??= new List<KnowledgeBaseSecurityTagItem>();

        NormalizeFolders(snapshot.Folders);

        foreach (var securityTag in snapshot.SecurityTags)
        {
            securityTag.AllowedGroupIds = NormalizeGroupIds(securityTag.AllowedGroupIds);
        }

        snapshot.Folders = snapshot.Folders
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(folder =>
            {
                folder.Articles = folder.Articles
                    .OrderBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return folder;
            })
            .ToList();

        snapshot.SecurityTags = snapshot.SecurityTags
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var json = JsonSerializer.Serialize(snapshot);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var blobClient = _containerClient.GetBlobClient(BlobName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<List<KnowledgeBaseFolderItem>> LoadFoldersAsync()
    {
        var snapshot = await LoadSnapshotAsync();
        return snapshot.Folders;
    }

    public async Task SaveFoldersAsync(List<KnowledgeBaseFolderItem> folders)
    {
        var snapshot = await LoadSnapshotAsync();
        snapshot.Folders = folders;
        await SaveSnapshotAsync(snapshot);
    }

    private static void NormalizeFolders(List<KnowledgeBaseFolderItem> folders)
    {
        foreach (var folder in folders)
        {
            folder.Articles ??= new List<KnowledgeBaseArticleItem>();
            folder.SecurityTagIds = folder.SecurityTagIds
                .Distinct()
                .ToList();
            folder.AllowedGroupIds = NormalizeGroupIds(folder.AllowedGroupIds);

            foreach (var article in folder.Articles)
            {
                article.SecurityTagIds = article.SecurityTagIds
                    .Distinct()
                    .ToList();
                article.AllowedGroupIds = NormalizeGroupIds(article.AllowedGroupIds);
            }
        }
    }

    private static List<string> NormalizeGroupIds(List<string>? groupIds)
    {
        return (groupIds ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
