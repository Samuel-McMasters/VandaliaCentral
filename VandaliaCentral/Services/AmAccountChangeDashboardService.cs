using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;
using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public sealed class AmAccountChangeDashboardService : IAmAccountChangeDashboardService
{
    private const string ContainerName = "amaccountchangedashboard";
    private const string BlobName = "pending-open-contract-account-changes.json";

    private static readonly List<AmAccountChangeDashboardItem> Pending = new();
    private static readonly object Sync = new();
    private static bool IsLoaded;

    private readonly GraphEmailService _email;
    private readonly EmailRoutingSettingsService _emailRoutingSettingsService;
    private readonly BlobContainerClient _containerClient;

    public AmAccountChangeDashboardService(
        GraphEmailService email,
        EmailRoutingSettingsService emailRoutingSettingsService,
        IConfiguration configuration)
    {
        _email = email;
        _emailRoutingSettingsService = emailRoutingSettingsService;

        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? configuration["AzureStorage:connectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is required.");

        _containerClient = new BlobContainerClient(connectionString, ContainerName);
        _containerClient.CreateIfNotExists();
    }

    public Task QueueOpenContractAccountsAsync(
        AmAssignmentChangeRequestModel model,
        IEnumerable<AmAssignmentCustomerLine> lines,
        string submittedByEmail,
        string submittedByName,
        string submissionId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        lock (Sync)
        {
            EnsureLoaded();

            foreach (var line in lines)
            {
                Pending.Add(new AmAccountChangeDashboardItem
                {
                    SubmissionId = submissionId,
                    SubmittedByEmail = submittedByEmail,
                    SubmittedByName = submittedByName,
                    SubmittedAtUtc = now,
                    ExecutiveName = model.ExecutiveName,
                    Location = model.Location,
                    SalespersonType = model.SalespersonType,
                    CurrentAmName = model.CurrentAmName,
                    CurrentAmSalesRepNumber = model.CurrentAmSalesRepNumber,
                    NewAmName = model.NewAmName,
                    NewAmSalesRepNumber = model.NewAmSalesRepNumber,
                    AccountNumber = line.AccountNumber,
                    CompanyName = line.CompanyName,
                    AssignOpenContracts = line.AssignOpenContracts,
                    ReferralAccount = line.ReferralAccount
                });
            }

            SaveUnsafe();
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<AmAccountChangeDashboardItem> GetPending()
    {
        lock (Sync)
        {
            EnsureLoaded();

            return Pending
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToList();
        }
    }

    public int GetPendingCount()
    {
        lock (Sync)
        {
            EnsureLoaded();
            return Pending.Count;
        }
    }

    public async Task ApproveAsync(string itemId, string approvedBy, CancellationToken ct = default)
    {
        var item = GetRequiredItem(itemId);
        var emailSettings = await _emailRoutingSettingsService.GetSettingsAsync(ct);

        if (string.IsNullOrWhiteSpace(emailSettings.AmStandardTo))
            throw new InvalidOperationException("TODO: Configure AmAssignmentChangeRequestEmail:StandardTo.");

        var model = ToRequestModel(item);
        var body = AmAssignmentChangeRequestEmailTemplateBuilder.BuildSubmissionHtmlBody(
            model,
            new List<AmAssignmentCustomerLine>
            {
                new()
                {
                    AccountNumber = item.AccountNumber,
                    CompanyName = item.CompanyName,
                    AssignOpenContracts = item.AssignOpenContracts,
                    ReferralAccount = item.ReferralAccount
                }
            },
            groupTitle: "Approved Open Contract Assignment",
            submittedBy: item.SubmittedByEmail,
            submissionId: item.SubmissionId,
            approvedBy: approvedBy);

        await _email.SendEmailHtmlAsyncStrict(emailSettings.AmStandardTo, "AM Assignment Change Request", body, ccEmail: null, ct);
        Remove(itemId);
    }


    public async Task ApproveWithoutContractsAsync(string itemId, string approvedBy, CancellationToken ct = default)
    {
        var item = GetRequiredItem(itemId);
        var emailSettings = await _emailRoutingSettingsService.GetSettingsAsync(ct);

        if (string.IsNullOrWhiteSpace(emailSettings.AmStandardTo))
            throw new InvalidOperationException("TODO: Configure AmAssignmentChangeRequestEmail:StandardTo.");

        var model = ToRequestModel(item);
        var body = AmAssignmentChangeRequestEmailTemplateBuilder.BuildSubmissionHtmlBody(
            model,
            new List<AmAssignmentCustomerLine>
            {
                new()
                {
                    AccountNumber = item.AccountNumber,
                    CompanyName = item.CompanyName,
                    AssignOpenContracts = false,
                    ReferralAccount = item.ReferralAccount
                }
            },
            groupTitle: "Approved Open Contract Assignment",
            submittedBy: item.SubmittedByEmail,
            submissionId: item.SubmissionId,
            approvedBy: approvedBy);

        await _email.SendEmailHtmlAsyncStrict(emailSettings.AmStandardTo, "AM Assignment Change Request", body, ccEmail: null, ct);
        Remove(itemId);
    }

    public async Task DenyAsync(string itemId, string deniedBy, CancellationToken ct = default)
    {
        var item = GetRequiredItem(itemId);

        if (string.IsNullOrWhiteSpace(item.SubmittedByEmail))
            throw new InvalidOperationException("Original submitter email is missing.");

        var body = AmAssignmentChangeRequestEmailTemplateBuilder.BuildDeniedHtmlBody(item, deniedBy);
        await _email.SendEmailHtmlAsyncStrict(item.SubmittedByEmail, "AM Assignment Change Request - Denied", body, ccEmail: null, ct);

        Remove(itemId);
    }

    private static AmAssignmentChangeRequestModel ToRequestModel(AmAccountChangeDashboardItem item)
    {
        return new AmAssignmentChangeRequestModel
        {
            ExecutiveName = item.ExecutiveName,
            Location = item.Location,
            SalespersonType = item.SalespersonType,
            CurrentAmName = item.CurrentAmName,
            CurrentAmSalesRepNumber = item.CurrentAmSalesRepNumber,
            NewAmName = item.NewAmName,
            NewAmSalesRepNumber = item.NewAmSalesRepNumber,
            Accounts = new List<AmAssignmentCustomerLine>()
        };
    }

    private AmAccountChangeDashboardItem GetRequiredItem(string itemId)
    {
        lock (Sync)
        {
            EnsureLoaded();

            var item = Pending.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
                throw new InvalidOperationException("Dashboard item not found or already processed.");

            return item;
        }
    }

    private void Remove(string itemId)
    {
        lock (Sync)
        {
            Pending.RemoveAll(x => x.Id == itemId);
            SaveUnsafe();
        }
    }

    private void EnsureLoaded()
    {
        if (IsLoaded)
            return;

        var blobClient = _containerClient.GetBlobClient(BlobName);

        try
        {
            if (!blobClient.Exists())
            {
                IsLoaded = true;
                return;
            }

            var download = blobClient.DownloadContent();
            var json = download.Value.Content.ToString();
            var loaded = string.IsNullOrWhiteSpace(json)
                ? new List<AmAccountChangeDashboardItem>()
                : JsonSerializer.Deserialize<List<AmAccountChangeDashboardItem>>(json) ?? new List<AmAccountChangeDashboardItem>();

            Pending.Clear();
            Pending.AddRange(loaded);
        }
        catch
        {
            Pending.Clear();
        }
        finally
        {
            IsLoaded = true;
        }
    }

    private void SaveUnsafe()
    {
        var payload = JsonSerializer.Serialize(Pending, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var blobClient = _containerClient.GetBlobClient(BlobName);
        blobClient.Upload(stream, overwrite: true);
    }
}
