using Microsoft.Extensions.Options;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public sealed class AmAssignmentChangeRequestSubmissionService : IAmAssignmentChangeRequestSubmissionService
{
    private readonly GraphEmailService _email;
    private readonly AmAssignmentChangeRequestEmailOptions _opts;
    private readonly IAmAccountChangeDashboardService _dashboardService;

    private const string SubjectLine = "AM Assignment Change Request";

    public AmAssignmentChangeRequestSubmissionService(
        GraphEmailService email,
        IOptions<AmAssignmentChangeRequestEmailOptions> opts,
        IAmAccountChangeDashboardService dashboardService)
    {
        _email = email;
        _opts = opts.Value;
        _dashboardService = dashboardService;
    }

    public async Task SubmitAsync(AmAssignmentChangeRequestModel model, string fromUserEmail, string submittedByName, CancellationToken ct = default)
    {
        var openLines = model.Accounts.Where(a => a.AssignOpenContracts).ToList();
        var standardLines = model.Accounts.Where(a => !a.AssignOpenContracts).ToList();

        // Validate config only for groups that actually exist
        if (openLines.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(_opts.OpenContractsTo))
                throw new InvalidOperationException("TODO: Configure AmAssignmentChangeRequestEmail:OpenContractsTo.");

            if (string.IsNullOrWhiteSpace(_opts.OpenContractsCc))
                throw new InvalidOperationException("TODO: Configure AmAssignmentChangeRequestEmail:OpenContractsCc (required when Assign Open Contracts is checked).");
        }

        if (standardLines.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(_opts.StandardTo))
                throw new InvalidOperationException("TODO: Configure AmAssignmentChangeRequestEmail:StandardTo.");
        }

        // Add a submission id to help spot duplicates if a user retries
        var submissionId = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();

        if (openLines.Count > 0)
        {
            var subject = SubjectLine;
            var body = AmAssignmentChangeRequestEmailTemplateBuilder.BuildSubmissionHtmlBody(model, openLines,
                groupTitle: "Accounts WITH Assign Open Contracts checked",
                submittedBy: fromUserEmail,
                submissionId: submissionId);

            await _email.SendEmailHtmlAsyncStrict(_opts.OpenContractsTo, subject, body, _opts.OpenContractsCc, ct);
        }

        if (standardLines.Count > 0)
        {
            var subject = SubjectLine;
            var body = AmAssignmentChangeRequestEmailTemplateBuilder.BuildSubmissionHtmlBody(model, standardLines,
                groupTitle: "Accounts WITHOUT Assign Open Contracts checked",
                submittedBy: fromUserEmail,
                submissionId: submissionId);

            await _email.SendEmailHtmlAsyncStrict(_opts.StandardTo, subject, body, ccEmail: null, ct);
        }

        if (openLines.Count > 0)
        {
            await _dashboardService.QueueOpenContractAccountsAsync(model, openLines, fromUserEmail, submittedByName, submissionId, ct);
        }
    }
}
