using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public class KnowledgeBaseService
{
    private readonly BlobContainerClient _containerClient;
    private const string LegacyBlobName = "knowledge-base.json";
    private const string FolderRootPrefix = "folders/";

    public KnowledgeBaseService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:connectionString"];
        _containerClient = new BlobContainerClient(connectionString, "knowledgebase");
        _containerClient.CreateIfNotExists();
    }

    public async Task<List<KnowledgeBaseFolderItem>> LoadFoldersAsync()
    {
        var folderBlobs = await ListBlobNamesAsync(FolderRootPrefix);

        if (folderBlobs.Count == 0)
        {
            return await LoadLegacyFoldersAsync();
        }

        var folders = await LoadFoldersFromFolderBlobsAsync(folderBlobs);

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

        var desiredBlobs = BuildDesiredBlobPayloads(orderedFolders);
        var existingBlobNames = await ListBlobNamesAsync(FolderRootPrefix);

        foreach (var (blobName, payload) in desiredBlobs)
        {
            await UploadJsonBlobAsync(blobName, payload);
        }

        var desiredBlobNames = desiredBlobs.Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var blobName in existingBlobNames.Where(name => !desiredBlobNames.Contains(name)))
        {
            await _containerClient.DeleteBlobIfExistsAsync(blobName);
        }

        await _containerClient.DeleteBlobIfExistsAsync(LegacyBlobName);
    }

    private async Task<List<KnowledgeBaseFolderItem>> LoadLegacyFoldersAsync()
    {
        var blobClient = _containerClient.GetBlobClient(LegacyBlobName);

        if (!await blobClient.ExistsAsync())
        {
            return new List<KnowledgeBaseFolderItem>();
        }

        var folders = await DownloadJsonBlobAsync<List<KnowledgeBaseFolderItem>>(LegacyBlobName)
            ?? new List<KnowledgeBaseFolderItem>();

        foreach (var folder in folders)
        {
            folder.Articles ??= new List<KnowledgeBaseArticleItem>();
        }

        return folders;
    }

    private async Task<List<KnowledgeBaseFolderItem>> LoadFoldersFromFolderBlobsAsync(List<string> blobNames)
    {
        var folderBlobNames = blobNames
            .Where(name => name.EndsWith("/folder.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var folderMap = new Dictionary<string, KnowledgeBaseFolderItem>(StringComparer.Ordinal);

        foreach (var folderBlobName in folderBlobNames)
        {
            var folder = await DownloadJsonBlobAsync<KnowledgeBaseFolderItem>(folderBlobName);
            if (folder is null)
            {
                continue;
            }

            folder.Articles ??= new List<KnowledgeBaseArticleItem>();
            folder.Articles.Clear();

            var folderKey = GetFolderKeyFromFolderBlob(folderBlobName);
            if (string.IsNullOrWhiteSpace(folderKey))
            {
                continue;
            }

            folderMap[folderKey] = folder;
        }

        var articleBlobNames = blobNames
            .Where(name => name.Contains("/articles/", StringComparison.OrdinalIgnoreCase))
            .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var articleBlobName in articleBlobNames)
        {
            var folderKey = GetFolderKeyFromArticleBlob(articleBlobName);
            if (string.IsNullOrWhiteSpace(folderKey) || !folderMap.TryGetValue(folderKey, out var folder))
            {
                continue;
            }

            var article = await DownloadJsonBlobAsync<KnowledgeBaseArticleItem>(articleBlobName);
            if (article is null)
            {
                continue;
            }

            folder.Articles.Add(article);
        }

        foreach (var folder in folderMap.Values)
        {
            folder.Articles = folder.Articles
                .OrderBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return folderMap.Values.ToList();
    }

    private Dictionary<string, string> BuildDesiredBlobPayloads(List<KnowledgeBaseFolderItem> folders)
    {
        var blobs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var folder in folders)
        {
            folder.Articles ??= new List<KnowledgeBaseArticleItem>();

            var folderKey = BuildNamedKey(folder.Name, folder.Id);
            var folderPrefix = $"{FolderRootPrefix}{folderKey}/";
            var folderBlobName = $"{folderPrefix}folder.json";

            var folderPayload = JsonSerializer.Serialize(new KnowledgeBaseFolderItem
            {
                Id = folder.Id,
                Name = folder.Name,
                CreatedOn = folder.CreatedOn,
                Articles = new List<KnowledgeBaseArticleItem>()
            });

            blobs[folderBlobName] = folderPayload;

            foreach (var article in folder.Articles)
            {
                var articleBlobName = $"{folderPrefix}articles/{BuildNamedKey(article.Title, article.Id)}.json";
                blobs[articleBlobName] = JsonSerializer.Serialize(article);
            }
        }

        return blobs;
    }

    private async Task<List<string>> ListBlobNamesAsync(string prefix)
    {
        var names = new List<string>();

        await foreach (BlobItem blob in _containerClient.GetBlobsAsync(prefix: prefix))
        {
            names.Add(blob.Name);
        }

        return names;
    }

    private async Task<T?> DownloadJsonBlobAsync<T>(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            return default;
        }

        var download = await blobClient.DownloadContentAsync();
        using var reader = new StreamReader(download.Value.Content.ToStream());
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json);
    }

    private async Task UploadJsonBlobAsync(string blobName, string payload)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    private static string GetFolderKeyFromFolderBlob(string folderBlobName)
    {
        if (!folderBlobName.StartsWith(FolderRootPrefix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var relativePath = folderBlobName[FolderRootPrefix.Length..];
        var slashIndex = relativePath.IndexOf('/');
        return slashIndex <= 0 ? string.Empty : relativePath[..slashIndex];
    }

    private static string GetFolderKeyFromArticleBlob(string articleBlobName)
    {
        if (!articleBlobName.StartsWith(FolderRootPrefix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var relativePath = articleBlobName[FolderRootPrefix.Length..];
        var slashIndex = relativePath.IndexOf('/');
        return slashIndex <= 0 ? string.Empty : relativePath[..slashIndex];
    }

    private static string BuildNamedKey(string name, Guid id)
    {
        var slug = SlugifyForBlobPath(name);
        return $"{slug}--{id:N}";
    }

    private static string SlugifyForBlobPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "untitled";
        }

        var builder = new StringBuilder();
        var lastWasSeparator = false;

        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasSeparator = false;
                continue;
            }

            if (c is ' ' or '-' or '_' or '.')
            {
                if (!lastWasSeparator)
                {
                    builder.Append('-');
                    lastWasSeparator = true;
                }
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "untitled" : slug;
    }
}
