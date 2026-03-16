using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public sealed class AmAccountChangeDashboardService : IAmAccountChangeDashboardService
{
    private readonly List<AmAccountChangeDashboardItem> _pending = new();
    private readonly object _sync = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AmAssignmentChangeRequestEmailOptions _opts;

    public AmAccountChangeDashboardService(
        IServiceScopeFactory scopeFactory,
        IOptions<AmAssignmentChangeRequestEmailOptions> opts)
    {
        _scopeFactory = scopeFactory;
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

        lock (_sync)
        {
            foreach (var line in lines)
            {
                _pending.Add(new AmAccountChangeDashboardItem
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
        lock (_sync)
        {
            return _pending
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToList();
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

        using var scope = _scopeFactory.CreateScope();
        var email = scope.ServiceProvider.GetRequiredService<GraphEmailService>();
        await email.SendEmailHtmlAsyncStrict(_opts.StandardTo, "AM Assignment Change Request", body, ccEmail: null, ct);
        Remove(itemId);
    }

    public async Task DenyAsync(string itemId, string deniedBy, CancellationToken ct = default)
    {
        var item = GetRequiredItem(itemId);

        if (string.IsNullOrWhiteSpace(item.SubmittedByEmail))
            throw new InvalidOperationException("Original submitter email is missing.");

        var body = AmAssignmentChangeRequestEmailTemplateBuilder.BuildDeniedHtmlBody(item, deniedBy);
        using var scope = _scopeFactory.CreateScope();
        var email = scope.ServiceProvider.GetRequiredService<GraphEmailService>();
        await email.SendEmailHtmlAsyncStrict(item.SubmittedByEmail, "AM Assignment Change Request - Denied", body, ccEmail: null, ct);

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
        lock (_sync)
        {
            var item = _pending.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
                throw new InvalidOperationException("Dashboard item not found or already processed.");

            return item;
        }
    }

    private void Remove(string itemId)
    {
        lock (_sync)
        {
            _pending.RemoveAll(x => x.Id == itemId);
        }
    }
}
