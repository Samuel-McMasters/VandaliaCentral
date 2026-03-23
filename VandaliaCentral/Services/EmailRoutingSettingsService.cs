using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public sealed class EmailRoutingSettingsService
{
    private const string ContainerName = "adminsettings";
    private const string BlobName = "email-routing.json";

    private readonly BlobContainerClient _containerClient;
    private readonly EmailRoutingSettings _fallback;

    public EmailRoutingSettingsService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? configuration["AzureStorage:connectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is required.");

        _containerClient = new BlobContainerClient(connectionString, ContainerName);
        _containerClient.CreateIfNotExists();

        _fallback = new EmailRoutingSettings
        {
            AmOpenContractsTo = configuration["AmAssignmentChangeRequestEmail:OpenContractsTo"] ?? string.Empty,
            AmOpenContractsCc = configuration["AmAssignmentChangeRequestEmail:OpenContractsCc"] ?? string.Empty,
            AmStandardTo = configuration["AmAssignmentChangeRequestEmail:StandardTo"] ?? string.Empty,
            EmployeeChangeTo = configuration["EmployeeChangeEmail:To"] ?? string.Empty,
            EmployeeChangeCc = configuration["EmployeeChangeEmail:Cc"] ?? string.Empty,
            EmployeeTerminationTo = configuration["EmployeeTerminationEmail:To"] ?? string.Empty,
            EmployeeTerminationCc = configuration["EmployeeTerminationEmail:Cc"] ?? string.Empty,
            FeedbackTo = configuration["FeedbackEmail:To"] ?? string.Empty,
            SupportTo = configuration["SupportTicketEmail:To"] ?? "support@vandaliarental.com",
            SupportCc = configuration["SupportTicketEmail:Cc"] ?? "sam.mcmasters@vandaliarental.com"
        };
    }

    public async Task<EmailRoutingSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(BlobName);

        if (!await blobClient.ExistsAsync(ct))
        {
            return Clone(_fallback);
        }

        try
        {
            var download = await blobClient.DownloadContentAsync(ct);
            using var reader = new StreamReader(download.Value.Content.ToStream());
            var json = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Clone(_fallback);
            }

            var loaded = JsonSerializer.Deserialize<EmailRoutingSettings>(json) ?? new EmailRoutingSettings();
            return MergeWithFallback(loaded);
        }
        catch
        {
            return Clone(_fallback);
        }
    }

    public async Task SaveSettingsAsync(EmailRoutingSettings settings, CancellationToken ct = default)
    {
        var normalized = Normalize(settings);

        Validate(normalized);

        var payload = JsonSerializer.Serialize(normalized, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var blobClient = _containerClient.GetBlobClient(BlobName);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }

    private EmailRoutingSettings MergeWithFallback(EmailRoutingSettings loaded)
    {
        return new EmailRoutingSettings
        {
            AmOpenContractsTo = string.IsNullOrWhiteSpace(loaded.AmOpenContractsTo) ? _fallback.AmOpenContractsTo : loaded.AmOpenContractsTo.Trim(),
            AmOpenContractsCc = string.IsNullOrWhiteSpace(loaded.AmOpenContractsCc) ? _fallback.AmOpenContractsCc : loaded.AmOpenContractsCc.Trim(),
            AmStandardTo = string.IsNullOrWhiteSpace(loaded.AmStandardTo) ? _fallback.AmStandardTo : loaded.AmStandardTo.Trim(),
            EmployeeChangeTo = string.IsNullOrWhiteSpace(loaded.EmployeeChangeTo) ? _fallback.EmployeeChangeTo : loaded.EmployeeChangeTo.Trim(),
            EmployeeChangeCc = string.IsNullOrWhiteSpace(loaded.EmployeeChangeCc) ? _fallback.EmployeeChangeCc : loaded.EmployeeChangeCc.Trim(),
            EmployeeTerminationTo = string.IsNullOrWhiteSpace(loaded.EmployeeTerminationTo) ? _fallback.EmployeeTerminationTo : loaded.EmployeeTerminationTo.Trim(),
            EmployeeTerminationCc = string.IsNullOrWhiteSpace(loaded.EmployeeTerminationCc) ? _fallback.EmployeeTerminationCc : loaded.EmployeeTerminationCc.Trim(),
            FeedbackTo = string.IsNullOrWhiteSpace(loaded.FeedbackTo) ? _fallback.FeedbackTo : loaded.FeedbackTo.Trim(),
            SupportTo = string.IsNullOrWhiteSpace(loaded.SupportTo) ? _fallback.SupportTo : loaded.SupportTo.Trim(),
            SupportCc = string.IsNullOrWhiteSpace(loaded.SupportCc) ? _fallback.SupportCc : loaded.SupportCc.Trim()
        };
    }

    private static EmailRoutingSettings Normalize(EmailRoutingSettings settings)
    {
        return new EmailRoutingSettings
        {
            AmOpenContractsTo = settings.AmOpenContractsTo.Trim(),
            AmOpenContractsCc = settings.AmOpenContractsCc.Trim(),
            AmStandardTo = settings.AmStandardTo.Trim(),
            EmployeeChangeTo = settings.EmployeeChangeTo.Trim(),
            EmployeeChangeCc = settings.EmployeeChangeCc.Trim(),
            EmployeeTerminationTo = settings.EmployeeTerminationTo.Trim(),
            EmployeeTerminationCc = settings.EmployeeTerminationCc.Trim(),
            FeedbackTo = settings.FeedbackTo.Trim(),
            SupportTo = settings.SupportTo.Trim(),
            SupportCc = settings.SupportCc.Trim()
        };
    }

    private static void Validate(EmailRoutingSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AmOpenContractsTo))
            throw new InvalidOperationException("AM Open Contracts To is required.");

        if (string.IsNullOrWhiteSpace(settings.AmStandardTo))
            throw new InvalidOperationException("AM Standard To is required.");

        if (string.IsNullOrWhiteSpace(settings.EmployeeChangeTo))
            throw new InvalidOperationException("Employee Change To is required.");

        if (string.IsNullOrWhiteSpace(settings.EmployeeTerminationTo))
            throw new InvalidOperationException("Employee Termination To is required.");

        if (string.IsNullOrWhiteSpace(settings.FeedbackTo))
            throw new InvalidOperationException("Feedback To is required.");

        if (string.IsNullOrWhiteSpace(settings.SupportTo))
            throw new InvalidOperationException("Support To is required.");
    }

    private static EmailRoutingSettings Clone(EmailRoutingSettings source)
    {
        return new EmailRoutingSettings
        {
            AmOpenContractsTo = source.AmOpenContractsTo,
            AmOpenContractsCc = source.AmOpenContractsCc,
            AmStandardTo = source.AmStandardTo,
            EmployeeChangeTo = source.EmployeeChangeTo,
            EmployeeChangeCc = source.EmployeeChangeCc,
            EmployeeTerminationTo = source.EmployeeTerminationTo,
            EmployeeTerminationCc = source.EmployeeTerminationCc,
            FeedbackTo = source.FeedbackTo,
            SupportTo = source.SupportTo,
            SupportCc = source.SupportCc
        };
    }
}
