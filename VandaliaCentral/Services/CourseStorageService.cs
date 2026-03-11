using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VandaliaCentral.Services;

public class BlobItemInfo
{
    public string BlobPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}

public class CourseExamSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
}

public class CourseContentReference
{
    [JsonPropertyName("blobPath")]
    public string BlobPath { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

public class CourseStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("bodyText")]
    public string BodyText { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Document";

    [JsonPropertyName("content")]
    public CourseContentReference Content { get; set; } = new();

    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;
}

public class Course
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("steps")]
    public List<CourseStep> Steps { get; set; } = new();
}

public class CourseSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public class CourseIndex
{
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("courses")]
    public List<CourseSummary> Courses { get; set; } = new();
}

public class CourseStorageService
{
    // Blob layout in training-school container:
    // - courses/{courseId}.json
    // - courses/index.json
    // - exams/{examId}.json
    private const string ContainerName = "training-school";
    private const string CoursesPrefix = "courses/";
    private const string CourseIndexPath = "courses/index.json";
    private const string ExamsPrefix = "exams/";
    private const string UserProfilesPrefix = "user-training-profiles/";
    private const string LinksPrefix = "links/";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly BlobContainerClient _containerClient;

    public CourseStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        _containerClient = new BlobContainerClient(connectionString, ContainerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<Course?> GetCourseAsync(string courseId)
    {
        var safeCourseId = Path.GetFileNameWithoutExtension(courseId);
        if (string.IsNullOrWhiteSpace(safeCourseId))
        {
            return null;
        }

        var blobClient = _containerClient.GetBlobClient($"{CoursesPrefix}{safeCourseId}.json");

        try
        {
            var content = await blobClient.DownloadContentAsync();
            return content.Value.Content.ToObjectFromJson<Course>(SerializerOptions);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<CourseSummary>> ListCoursesAsync()
    {
        var index = await LoadCourseIndexAsync();
        return index.Courses
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToList();
    }

    public async Task SaveCourseAsync(Course course)
    {
        NormalizeAndValidateCourse(course);

        course.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var courseBlobPath = $"{CoursesPrefix}{course.Id}.json";
        await SaveJsonWithRetryAsync(courseBlobPath, course);

        await SaveIndexWithRetryAsync(course);
    }

    public async Task DeleteCourseAsync(string courseId)
    {
        var safeCourseId = Path.GetFileNameWithoutExtension(courseId);
        if (string.IsNullOrWhiteSpace(safeCourseId))
        {
            return;
        }

        await _containerClient.DeleteBlobIfExistsAsync($"{CoursesPrefix}{safeCourseId}.json");

        await SaveIndexWithRetryAsync(index =>
        {
            index.Courses.RemoveAll(c => c.Id.Equals(safeCourseId, StringComparison.OrdinalIgnoreCase));
            index.UpdatedAtUtc = DateTimeOffset.UtcNow;
        });
    }

    public async Task<List<BlobItemInfo>> ListDocumentBlobsAsync()
    {
        var documentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx"
        };

        return await ListAssetsByExtensionAsync(documentExtensions);
    }

    public async Task<List<BlobItemInfo>> ListVideoBlobsAsync()
    {
        var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".mov", ".m4v", ".avi", ".wmv"
        };

        return await ListAssetsByExtensionAsync(videoExtensions);
    }

    public async Task<List<CourseExamSummary>> ListExamsAsync()
    {
        var exams = new List<CourseExamSummary>();

        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: ExamsPrefix))
        {
            if (!blob.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blob.Name);
                var content = await blobClient.DownloadContentAsync();

                using var doc = JsonDocument.Parse(content.Value.Content.ToStream());
                var root = doc.RootElement;

                var id = root.TryGetProperty("Id", out var idProp)
                    ? idProp.GetString()
                    : root.TryGetProperty("id", out var idPropLower)
                        ? idPropLower.GetString()
                        : null;

                var title = root.TryGetProperty("Title", out var titleProp)
                    ? titleProp.GetString()
                    : root.TryGetProperty("title", out var titlePropLower)
                        ? titlePropLower.GetString()
                        : null;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                var preferredBlobPath = blob.Name.EndsWith($"{id}.json", StringComparison.OrdinalIgnoreCase)
                    ? $"{ExamsPrefix}{id}.json"
                    : blob.Name;

                exams.Add(new CourseExamSummary
                {
                    Id = id,
                    Title = title,
                    BlobPath = preferredBlobPath
                });
            }
            catch
            {
                // Skip malformed exam blobs.
            }
        }

        return exams.OrderBy(e => e.Title).ToList();
    }

    public async Task<List<BlobItemInfo>> ListLinksAsync()
    {
        var links = new List<BlobItemInfo>();

        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: LinksPrefix))
        {
            if (!blob.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var blobClient = _containerClient.GetBlobClient(blob.Name);

            try
            {
                var content = await blobClient.DownloadContentAsync();
                var trainingLink = content.Value.Content.ToObjectFromJson<TrainingLink>(SerializerOptions);
                if (trainingLink == null)
                {
                    continue;
                }

                var displayName = string.IsNullOrWhiteSpace(trainingLink.PlaceholderText)
                    ? Path.GetFileNameWithoutExtension(blob.Name)
                    : trainingLink.PlaceholderText;

                links.Add(new BlobItemInfo
                {
                    BlobPath = blob.Name,
                    DisplayName = displayName,
                    ContentType = "application/json"
                });
            }
            catch
            {
                // Skip malformed link blobs.
            }
        }

        return links.OrderBy(l => l.DisplayName).ToList();
    }

    private async Task<List<BlobItemInfo>> ListAssetsByExtensionAsync(HashSet<string> allowedExtensions)
    {
        var excludedPrefixes = new[] { ExamsPrefix, CoursesPrefix, UserProfilesPrefix, LinksPrefix };
        var blobs = new List<BlobItemInfo>();

        await foreach (var blob in _containerClient.GetBlobsAsync())
        {
            if (excludedPrefixes.Any(prefix => blob.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var extension = Path.GetExtension(blob.Name);
            if (!allowedExtensions.Contains(extension))
            {
                continue;
            }

            blobs.Add(new BlobItemInfo
            {
                BlobPath = blob.Name,
                DisplayName = Path.GetFileNameWithoutExtension(blob.Name),
                ContentType = blob.Properties.ContentType
            });
        }

        return blobs.OrderBy(b => b.DisplayName).ToList();
    }

    private static void NormalizeAndValidateCourse(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.Id))
        {
            course.Id = Guid.NewGuid().ToString("N");
        }

        if (string.IsNullOrWhiteSpace(course.Title))
        {
            throw new InvalidOperationException("Course title is required.");
        }

        var normalizedSteps = (course.Steps ?? new List<CourseStep>())
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Title)
            .ToList();

        for (var i = 0; i < normalizedSteps.Count; i++)
        {
            var step = normalizedSteps[i];
            step.Id = string.IsNullOrWhiteSpace(step.Id) ? Guid.NewGuid().ToString("N") : step.Id;
            step.Order = i + 1;

            if (string.IsNullOrWhiteSpace(step.Title))
            {
                throw new InvalidOperationException($"Step {step.Order} must include a title.");
            }

            if (string.IsNullOrWhiteSpace(step.Type)
                || !new[] { "Document", "Video", "Exam", "Link" }.Contains(step.Type, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Step {step.Order} has an invalid type.");
            }

            if (string.IsNullOrWhiteSpace(step.Content?.BlobPath))
            {
                throw new InvalidOperationException($"Step {step.Order} must include content.");
            }

            if (step.Type.Equals("Exam", StringComparison.OrdinalIgnoreCase)
                && !step.Content.BlobPath.StartsWith(ExamsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Step {step.Order} is an exam and must reference an exams/ blob.");
            }

            if (step.Type.Equals("Document", StringComparison.OrdinalIgnoreCase)
                && step.Content.BlobPath.StartsWith(ExamsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Step {step.Order} is a document and cannot reference an exam blob.");
            }

            if (step.Type.Equals("Video", StringComparison.OrdinalIgnoreCase)
                && step.Content.BlobPath.StartsWith(ExamsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Step {step.Order} is a video and cannot reference an exam blob.");
            }

            if (step.Type.Equals("Link", StringComparison.OrdinalIgnoreCase)
                && !step.Content.BlobPath.StartsWith(LinksPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Step {step.Order} is a link and must reference a links/ blob.");
            }
        }

        course.Steps = normalizedSteps;
    }

    private async Task<CourseIndex> LoadCourseIndexAsync()
    {
        var blobClient = _containerClient.GetBlobClient(CourseIndexPath);

        try
        {
            var content = await blobClient.DownloadContentAsync();
            return content.Value.Content.ToObjectFromJson<CourseIndex>(SerializerOptions) ?? new CourseIndex();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new CourseIndex();
        }
    }

    private async Task SaveIndexWithRetryAsync(Course course)
    {
        await SaveIndexWithRetryAsync(index =>
        {
            var existing = index.Courses.FirstOrDefault(c => c.Id == course.Id);
            var summary = new CourseSummary
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                IsPublished = course.IsPublished,
                UpdatedAtUtc = course.UpdatedAtUtc
            };

            if (existing == null)
            {
                index.Courses.Add(summary);
            }
            else
            {
                existing.Title = summary.Title;
                existing.Description = summary.Description;
                existing.IsPublished = summary.IsPublished;
                existing.UpdatedAtUtc = summary.UpdatedAtUtc;
            }

            index.UpdatedAtUtc = DateTimeOffset.UtcNow;
        });
    }

    private async Task SaveIndexWithRetryAsync(Action<CourseIndex> mutate)
    {
        var blobClient = _containerClient.GetBlobClient(CourseIndexPath);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            ETag? etag = null;
            var index = new CourseIndex();

            try
            {
                var download = await blobClient.DownloadContentAsync();
                etag = download.Value.Details.ETag;
                index = download.Value.Content.ToObjectFromJson<CourseIndex>(SerializerOptions) ?? new CourseIndex();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                etag = null;
            }

            mutate(index);
            var json = JsonSerializer.Serialize(index, SerializerOptions);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            try
            {
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
                };

                if (etag.HasValue)
                {
                    options.Conditions = new BlobRequestConditions { IfMatch = etag.Value };
                }

                await blobClient.UploadAsync(stream, options);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // Lost race; retry by reloading latest index and applying mutation.
            }
        }

        throw new InvalidOperationException("Unable to save courses index due to concurrent updates.");
    }

    private async Task SaveJsonWithRetryAsync<T>(string blobPath, T payload)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            ETag? etag = null;

            try
            {
                var properties = await blobClient.GetPropertiesAsync();
                etag = properties.Value.ETag;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                etag = null;
            }

            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            try
            {
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
                };

                if (etag.HasValue)
                {
                    options.Conditions = new BlobRequestConditions { IfMatch = etag.Value };
                }

                await blobClient.UploadAsync(stream, options);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // Lost race; retry and last write wins.
            }
        }

        throw new InvalidOperationException($"Unable to save blob '{blobPath}' due to concurrent updates.");
    }
}
