using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;
using System.Text.Json;

namespace VandaliaCentral.Services
{
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

    public class TrainingLink
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string PlaceholderText { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public class UserLearningAssignment
    {
        public string AssignmentId { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public List<string> CompletedCourseStepIds { get; set; } = new();
        public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAtUtc { get; set; }
    }

    public class CompleteCourseStepResult
    {
        public bool Success { get; set; }
        public bool AssignmentCompleted { get; set; }
        public List<string> CompletedStepIds { get; set; } = new();
    }

    public class UserTrainingProfile
    {
        public string UserId { get; set; } = string.Empty;
        public List<UserLearningAssignment> ActiveAssignedLearning { get; set; } = new();
        public List<UserLearningAssignment> LearningHistory { get; set; } = new();
    }

    public class AssignLearningRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public string? ContentType { get; set; }
    }

    public class TrainingDocumentService
    {
        private const string ContainerName = "training-school";
        private const string ExamFolderPrefix = "exams/";
        private const string UserTrainingProfilePrefix = "user-training-profiles/";
        private const string CourseFolderPrefix = "courses/";
        private const string LinkFolderPrefix = "links/";
        private const long MaxFileSizeBytes = 500L * 1024 * 1024;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".mp4"
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
                if (blob.Name.StartsWith(ExamFolderPrefix, StringComparison.OrdinalIgnoreCase)
                    || blob.Name.StartsWith(UserTrainingProfilePrefix, StringComparison.OrdinalIgnoreCase)
                    || blob.Name.StartsWith(CourseFolderPrefix, StringComparison.OrdinalIgnoreCase)
                    || blob.Name.StartsWith(LinkFolderPrefix, StringComparison.OrdinalIgnoreCase))
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

        public async Task<TrainingExam?> GetExamAsync(string examId)
        {
            var safeExamId = Path.GetFileNameWithoutExtension(examId);
            if (string.IsNullOrWhiteSpace(safeExamId))
            {
                return null;
            }

            var blobClient = _containerClient.GetBlobClient($"{ExamFolderPrefix}{safeExamId}.json");

            try
            {
                var download = await blobClient.DownloadContentAsync();
                return download.Value.Content.ToObjectFromJson<TrainingExam>();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task DeleteExamAsync(string examId)
        {
            var safeExamId = Path.GetFileNameWithoutExtension(examId);
            if (string.IsNullOrWhiteSpace(safeExamId))
            {
                return;
            }

            var examBlobPath = $"{ExamFolderPrefix}{safeExamId}.json";

            await _containerClient.DeleteBlobIfExistsAsync(examBlobPath);
            await RemoveActiveAssignmentsByBlobPathAsync(examBlobPath);
        }


        public async Task<List<TrainingLink>> ListLinksAsync()
        {
            var links = new List<TrainingLink>();

            await foreach (var blob in _containerClient.GetBlobsAsync(prefix: LinkFolderPrefix))
            {
                var blobClient = _containerClient.GetBlobClient(blob.Name);

                try
                {
                    var download = await blobClient.DownloadContentAsync();
                    var link = download.Value.Content.ToObjectFromJson<TrainingLink>();
                    if (link == null || string.IsNullOrWhiteSpace(link.Id))
                    {
                        continue;
                    }

                    links.Add(link);
                }
                catch
                {
                    // Skip malformed link files.
                }
            }

            return links
                .OrderBy(l => l.PlaceholderText)
                .ToList();
        }

        public async Task<TrainingLink?> GetLinkAsync(string linkId)
        {
            var safeLinkId = Path.GetFileNameWithoutExtension(linkId);
            if (string.IsNullOrWhiteSpace(safeLinkId))
            {
                return null;
            }

            var blobClient = _containerClient.GetBlobClient($"{LinkFolderPrefix}{safeLinkId}.json");

            try
            {
                var download = await blobClient.DownloadContentAsync();
                return download.Value.Content.ToObjectFromJson<TrainingLink>();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task SaveLinkAsync(TrainingLink link)
        {
            if (string.IsNullOrWhiteSpace(link.PlaceholderText))
            {
                throw new InvalidOperationException("Placeholder text is required.");
            }

            if (string.IsNullOrWhiteSpace(link.Url)
                || !Uri.TryCreate(link.Url.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("A valid http(s) URL is required.");
            }

            if (string.IsNullOrWhiteSpace(link.Id))
            {
                link.Id = Guid.NewGuid().ToString("N");
            }

            link.PlaceholderText = link.PlaceholderText.Trim();
            link.Url = uri.ToString();
            link.CreatedAtUtc = DateTimeOffset.UtcNow;

            var blobName = $"{LinkFolderPrefix}{link.Id}.json";
            var blobClient = _containerClient.GetBlobClient(blobName);

            var json = JsonSerializer.Serialize(link, SerializerOptions);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            await blobClient.UploadAsync(stream, overwrite: true);
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "application/json" });
        }

        public async Task DeleteLinkAsync(string linkId)
        {
            var safeLinkId = Path.GetFileNameWithoutExtension(linkId);
            if (string.IsNullOrWhiteSpace(safeLinkId))
            {
                return;
            }

            var linkBlobPath = $"{LinkFolderPrefix}{safeLinkId}.json";

            await _containerClient.DeleteBlobIfExistsAsync(linkBlobPath);
            await RemoveActiveAssignmentsByBlobPathAsync(linkBlobPath);
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

            if (exam.PassingScorePercent < 1 || exam.PassingScorePercent > 100)
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

            var json = JsonSerializer.Serialize(exam, SerializerOptions);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            await blobClient.UploadAsync(stream, overwrite: true);
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "application/json" });
        }

        public async Task<UserTrainingProfile> GetUserTrainingProfileAsync(string userId)
        {
            var safeUserId = Path.GetFileNameWithoutExtension(userId);
            if (string.IsNullOrWhiteSpace(safeUserId))
            {
                return new UserTrainingProfile();
            }

            var blobClient = _containerClient.GetBlobClient($"{UserTrainingProfilePrefix}{safeUserId}.json");

            try
            {
                var download = await blobClient.DownloadContentAsync();
                return download.Value.Content.ToObjectFromJson<UserTrainingProfile>()
                    ?? new UserTrainingProfile { UserId = safeUserId };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return new UserTrainingProfile { UserId = safeUserId };
            }
        }

        public async Task<List<string>> GetUserIdsWithActiveAssignedLearningAsync()
        {
            var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var profileBlob in _containerClient.GetBlobsAsync(prefix: UserTrainingProfilePrefix))
            {
                var profileBlobClient = _containerClient.GetBlobClient(profileBlob.Name);

                UserTrainingProfile? profile;
                try
                {
                    var download = await profileBlobClient.DownloadContentAsync();
                    profile = download.Value.Content.ToObjectFromJson<UserTrainingProfile>();
                }
                catch
                {
                    continue;
                }

                if (profile?.ActiveAssignedLearning == null || profile.ActiveAssignedLearning.Count == 0)
                {
                    continue;
                }

                var safeUserId = Path.GetFileNameWithoutExtension(profile.UserId);
                if (string.IsNullOrWhiteSpace(safeUserId))
                {
                    var fileName = Path.GetFileName(profileBlob.Name);
                    safeUserId = Path.GetFileNameWithoutExtension(fileName);
                }

                if (!string.IsNullOrWhiteSpace(safeUserId))
                {
                    userIds.Add(safeUserId);
                }
            }

            return userIds.ToList();
        }

        public async Task AssignLearningToUserAsync(AssignLearningRequest request)
        {
            var safeUserId = Path.GetFileNameWithoutExtension(request.UserId);
            if (string.IsNullOrWhiteSpace(safeUserId))
            {
                throw new InvalidOperationException("User id is required for assignment.");
            }

            if (string.IsNullOrWhiteSpace(request.Title)
                || string.IsNullOrWhiteSpace(request.ItemType)
                || string.IsNullOrWhiteSpace(request.BlobPath))
            {
                throw new InvalidOperationException("Assignment must include title, type, and blob path.");
            }

            var profile = await GetUserTrainingProfileAsync(safeUserId);
            profile.UserId = safeUserId;

            profile.ActiveAssignedLearning.Add(new UserLearningAssignment
            {
                AssignmentId = Guid.NewGuid().ToString("N"),
                Title = request.Title,
                ItemType = request.ItemType,
                BlobPath = request.BlobPath,
                ContentType = request.ContentType,
                AssignedAtUtc = DateTimeOffset.UtcNow
            });

            await SaveUserTrainingProfileAsync(profile);
        }

        public async Task<bool> CompleteAssignmentForUserAsync(string userId, string assignmentId)
        {
            var safeUserId = Path.GetFileNameWithoutExtension(userId);
            if (string.IsNullOrWhiteSpace(safeUserId) || string.IsNullOrWhiteSpace(assignmentId))
            {
                return false;
            }

            var profile = await GetUserTrainingProfileAsync(safeUserId);
            profile.UserId = safeUserId;

            var assignment = profile.ActiveAssignedLearning.FirstOrDefault(a =>
                string.Equals(a.AssignmentId, assignmentId, StringComparison.OrdinalIgnoreCase));

            if (assignment == null)
            {
                return false;
            }

            profile.ActiveAssignedLearning.Remove(assignment);
            assignment.CompletedAtUtc = DateTimeOffset.UtcNow;
            profile.LearningHistory.Add(assignment);

            await SaveUserTrainingProfileAsync(profile);
            return true;
        }

        public async Task<bool> UnassignLearningFromUserAsync(string userId, string assignmentId, string? blobPath = null, DateTimeOffset? assignedAtUtc = null)
        {
            var safeUserId = Path.GetFileNameWithoutExtension(userId);
            if (string.IsNullOrWhiteSpace(safeUserId))
            {
                return false;
            }

            var profile = await GetUserTrainingProfileAsync(safeUserId);
            profile.UserId = safeUserId;

            var assignment = profile.ActiveAssignedLearning.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(assignmentId)
                && string.Equals(a.AssignmentId, assignmentId, StringComparison.OrdinalIgnoreCase));

            if (assignment == null
                && !string.IsNullOrWhiteSpace(blobPath)
                && assignedAtUtc.HasValue)
            {
                assignment = profile.ActiveAssignedLearning.FirstOrDefault(a =>
                    string.Equals(a.BlobPath, blobPath, StringComparison.OrdinalIgnoreCase)
                    && a.AssignedAtUtc.UtcTicks == assignedAtUtc.Value.UtcTicks);
            }

            if (assignment == null)
            {
                return false;
            }

            profile.ActiveAssignedLearning.Remove(assignment);
            await SaveUserTrainingProfileAsync(profile);
            return true;
        }

        public async Task<CompleteCourseStepResult> CompleteCourseStepForUserAsync(string userId, string assignmentId, string stepId, int totalRequiredSteps)
        {
            var result = new CompleteCourseStepResult();
            var safeUserId = Path.GetFileNameWithoutExtension(userId);
            var safeStepId = Path.GetFileNameWithoutExtension(stepId);

            if (string.IsNullOrWhiteSpace(safeUserId)
                || string.IsNullOrWhiteSpace(assignmentId)
                || string.IsNullOrWhiteSpace(safeStepId)
                || totalRequiredSteps <= 0)
            {
                return result;
            }

            var profile = await GetUserTrainingProfileAsync(safeUserId);
            profile.UserId = safeUserId;

            var assignment = profile.ActiveAssignedLearning.FirstOrDefault(a =>
                string.Equals(a.AssignmentId, assignmentId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.ItemType, "Course", StringComparison.OrdinalIgnoreCase));

            if (assignment == null)
            {
                return result;
            }

            assignment.CompletedCourseStepIds ??= new List<string>();

            if (!assignment.CompletedCourseStepIds.Contains(safeStepId, StringComparer.OrdinalIgnoreCase))
            {
                assignment.CompletedCourseStepIds.Add(safeStepId);
            }

            result.CompletedStepIds = assignment.CompletedCourseStepIds.ToList();

            if (assignment.CompletedCourseStepIds.Count >= totalRequiredSteps)
            {
                profile.ActiveAssignedLearning.Remove(assignment);
                assignment.CompletedAtUtc = DateTimeOffset.UtcNow;
                profile.LearningHistory.Add(assignment);
                result.AssignmentCompleted = true;
            }

            await SaveUserTrainingProfileAsync(profile);
            result.Success = true;
            return result;
        }

        public async Task SaveUserTrainingProfileAsync(UserTrainingProfile profile)
        {
            var safeUserId = Path.GetFileNameWithoutExtension(profile.UserId);
            if (string.IsNullOrWhiteSpace(safeUserId))
            {
                throw new InvalidOperationException("User id is required for training profile save.");
            }

            profile.UserId = safeUserId;

            var blobClient = _containerClient.GetBlobClient($"{UserTrainingProfilePrefix}{safeUserId}.json");
            var json = JsonSerializer.Serialize(profile, SerializerOptions);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            await blobClient.UploadAsync(stream, overwrite: true);
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "application/json" });
        }

        public async Task UploadDocumentAsync(IBrowserFile file)
        {
            var safeFileName = Path.GetFileName(file.Name);
            var extension = Path.GetExtension(safeFileName);

            if (string.IsNullOrWhiteSpace(safeFileName) || !AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Unsupported file type. Please upload a .pdf or .mp4 file.");
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
            await RemoveActiveAssignmentsByBlobPathAsync(safeFileName);
        }

        public async Task RemoveActiveAssignmentsByBlobPathAsync(string blobPath)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                return;
            }

            await foreach (var profileBlob in _containerClient.GetBlobsAsync(prefix: UserTrainingProfilePrefix))
            {
                var profileBlobClient = _containerClient.GetBlobClient(profileBlob.Name);

                UserTrainingProfile? profile;
                try
                {
                    var download = await profileBlobClient.DownloadContentAsync();
                    profile = download.Value.Content.ToObjectFromJson<UserTrainingProfile>();
                }
                catch
                {
                    continue;
                }

                if (profile == null)
                {
                    continue;
                }

                var removed = profile.ActiveAssignedLearning.RemoveAll(assignment =>
                    string.Equals(assignment.BlobPath, blobPath, StringComparison.OrdinalIgnoreCase));

                if (removed <= 0)
                {
                    continue;
                }

                profile.UserId = Path.GetFileNameWithoutExtension(profile.UserId);
                if (string.IsNullOrWhiteSpace(profile.UserId))
                {
                    profile.UserId = Path.GetFileNameWithoutExtension(profileBlob.Name);
                }

                await SaveUserTrainingProfileAsync(profile);
            }
        }
    }
}
