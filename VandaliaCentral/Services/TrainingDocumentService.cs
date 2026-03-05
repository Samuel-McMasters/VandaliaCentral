using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace VandaliaCentral.Services;

public class TrainingDocumentInfo
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long? SizeInBytes { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

public class TrainingDocumentService
{
    private const string ContainerName = "training-school";
    private const long MaxFileSizeBytes = 500L * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv",
        ".mp4", ".mov", ".avi", ".wmv", ".m4v", ".webm"
    };

    private readonly BlobContainerClient _containerClient;

    public TrainingDocumentService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        _containerClient = new BlobContainerClient(connectionString, ContainerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<List<TrainingDocumentInfo>> ListDocumentsAsync()
    {
        var documents = new List<TrainingDocumentInfo>();

        await foreach (var blob in _containerClient.GetBlobsAsync())
        {
            documents.Add(new TrainingDocumentInfo
            {
                FileName = blob.Name,
                Url = _containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                SizeInBytes = blob.Properties.ContentLength,
                LastModified = blob.Properties.LastModified
            });
        }

        return documents
            .OrderByDescending(d => d.LastModified ?? DateTimeOffset.MinValue)
            .ToList();
    }

    public async Task UploadDocumentAsync(IBrowserFile file)
    {
        var safeFileName = Path.GetFileName(file.Name);
        var extension = Path.GetExtension(safeFileName);

        if (string.IsNullOrWhiteSpace(safeFileName) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type. Please upload a document or video file.");
        }

        var blobClient = _containerClient.GetBlobClient(safeFileName);
        await using var stream = file.OpenReadStream(maxAllowedSize: MaxFileSizeBytes);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await blobClient.UploadAsync(stream, overwrite: true);
        await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType });
    }

    public async Task<BlobDownloadStreamingResult?> DownloadDocumentAsync(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return null;
        }

        var blobClient = _containerClient.GetBlobClient(safeFileName);

        try
        {
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteDocumentAsync(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return;
        }

        await _containerClient.DeleteBlobIfExistsAsync(safeFileName);
    }
}
