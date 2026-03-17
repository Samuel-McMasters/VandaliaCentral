using Microsoft.Extensions.Options;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public sealed class AmAccountChangeDashboardService : IAmAccountChangeDashboardService
{
    private static readonly List<AmAccountChangeDashboardItem> Pending = new();
    private static readonly object Sync = new();

    private readonly GraphEmailService _email;
    private readonly AmAssignmentChangeRequestEmailOptions _opts;

    public AmAccountChangeDashboardService(
        GraphEmailService email,
        IOptions<AmAssignmentChangeRequestEmailOptions> opts)
    {
        _email = email;
        _opts = opts.Value;
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
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<AmAccountChangeDashboardItem> GetPending()
    {
        lock (Sync)
        {
            return Pending
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToList();
        }
    }

    public int GetPendingCount()
    {
        lock (Sync)
        {
            return Pending.Count;
        }
    }

    public async Task ApproveAsync(string itemId, string approvedBy, CancellationToken ct = default)
    {
        var item = GetRequiredItem(itemId);

        if (string.IsNullOrWhiteSpace(_opts.StandardTo))
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

        await _email.SendEmailHtmlAsyncStrict(_opts.StandardTo, "AM Assignment Change Request", body, ccEmail: null, ct);
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
        }
    }
}
