using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using System.Text.Json;


namespace VandaliaCentral.Services;

public class TrainingDocumentInfo
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long? SizeInBytes { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

public class TrainingExam
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public int PassingScorePercent { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<TrainingExamQuestion> Questions { get; set; } = new();
}

public class TrainingExamQuestion
{
    public string QuestionType { get; set; } = "multiple-choice";
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public List<int> CorrectOptionIndexes { get; set; } = new();
}

public class TrainingExamSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int PassingScorePercent { get; set; }
    public int QuestionCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class TrainingDocumentService
{
    private const string ContainerName = "training-school";
    private const string ExamFolderPrefix = "exams/";
    private const long MaxFileSizeBytes = 500L * 1024 * 1024;

    private static readonly JsonSerializerOptions ExamSerializerOptions = new()
    {
        WriteIndented = true
    };


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
            if (blob.Name.StartsWith(ExamFolderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }


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

    public async Task<List<TrainingExamSummary>> ListExamsAsync()
    {
        var exams = new List<TrainingExamSummary>();

        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: ExamFolderPrefix))
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);

            try
            {
                var download = await blobClient.DownloadContentAsync();
                var exam = download.Value.Content.ToObjectFromJson<TrainingExam>();

                if (exam == null)
                {
                    continue;
                }

                exams.Add(new TrainingExamSummary
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    PassingScorePercent = exam.PassingScorePercent,
                    QuestionCount = exam.Questions.Count,
                    CreatedAtUtc = exam.CreatedAtUtc
                });
            }
            catch
            {
                // Skip malformed exam files.
            }
        }

        return exams
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToList();
    }

    public async Task SaveExamAsync(TrainingExam exam)
    {
        if (string.IsNullOrWhiteSpace(exam.Title))
        {
            throw new InvalidOperationException("Exam title is required.");
        }

        if (exam.Questions.Count == 0)
        {
            throw new InvalidOperationException("At least one question is required.");
        }

        if (exam.PassingScorePercent is < 1 or > 100)
        {
            throw new InvalidOperationException("Passing score must be between 1 and 100.");
        }

        if (string.IsNullOrWhiteSpace(exam.Id))
        {
            exam.Id = Guid.NewGuid().ToString("N");
        }

        exam.CreatedAtUtc = DateTimeOffset.UtcNow;

        var blobName = $"{ExamFolderPrefix}{exam.Id}.json";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var json = JsonSerializer.Serialize(exam, ExamSerializerOptions);
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        await blobClient.UploadAsync(stream, overwrite: true);
        await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "application/json" });
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
