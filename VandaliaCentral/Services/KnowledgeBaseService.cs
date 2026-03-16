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

    public async Task<List<KnowledgeBaseFolderItem>> LoadFoldersAsync()
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);

        if (!await blobClient.ExistsAsync())
        {
            return new List<KnowledgeBaseFolderItem>();
        }

        var download = await blobClient.DownloadContentAsync();
        using var reader = new StreamReader(download.Value.Content.ToStream());
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<KnowledgeBaseFolderItem>();
        }

        var folders = JsonSerializer.Deserialize<List<KnowledgeBaseFolderItem>>(json) ?? new List<KnowledgeBaseFolderItem>();

        foreach (var folder in folders)
        {
            folder.Articles ??= new List<KnowledgeBaseArticleItem>();
        }

        return folders
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SaveFoldersAsync(List<KnowledgeBaseFolderItem> folders)
    {
        var orderedFolders = folders
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(folder =>
            {
                folder.Articles = folder.Articles
                    .OrderBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return folder;
            })
            .ToList();

        var json = JsonSerializer.Serialize(orderedFolders);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var blobClient = _containerClient.GetBlobClient(BlobName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
