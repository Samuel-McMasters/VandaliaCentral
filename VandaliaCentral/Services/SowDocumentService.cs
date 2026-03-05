using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace VandaliaCentral.Services;

public class SowDocumentInfo
{
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset? LastModified { get; set; }
}

public class SowDocumentService
{
    private const string ContainerName = "sow-documents";
    private readonly BlobContainerClient _containerClient;

    public SowDocumentService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        _containerClient = new BlobContainerClient(connectionString, ContainerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<List<SowDocumentInfo>> ListDocumentsAsync()
    {
        var documents = new List<SowDocumentInfo>();

        await foreach (var blob in _containerClient.GetBlobsAsync())
        {
            if (!blob.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            documents.Add(new SowDocumentInfo
            {
                FileName = blob.Name,
                Title = Path.GetFileNameWithoutExtension(blob.Name),
                Url = _containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                LastModified = blob.Properties.LastModified
            });
        }

        return documents
            .OrderByDescending(d => d.LastModified ?? DateTimeOffset.MinValue)
            .ToList();
    }

    public async Task UploadDocumentAsync(IBrowserFile file)
    {
        if (!file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only PDF files are allowed.");
        }

        var fileName = $"{Path.GetFileNameWithoutExtension(file.Name)}.pdf";
        var blobClient = _containerClient.GetBlobClient(fileName);

        await using var stream = file.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024);
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "application/pdf" }, overwrite: true);
    }

    public async Task DeleteDocumentAsync(string fileName)
    {
        await _containerClient.DeleteBlobIfExistsAsync(fileName);
    }
}
