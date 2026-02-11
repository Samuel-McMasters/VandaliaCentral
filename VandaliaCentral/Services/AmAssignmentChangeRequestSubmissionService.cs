using System.Net;
using System.Text;

using Microsoft.Extensions.Options;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public sealed class AmAssignmentChangeRequestSubmissionService : IAmAssignmentChangeRequestSubmissionService
{
    private readonly GraphEmailService _email;
    private readonly AmAssignmentChangeRequestEmailOptions _opts;

    public AmAssignmentChangeRequestSubmissionService(
        GraphEmailService email,
        IOptions<AmAssignmentChangeRequestEmailOptions> opts)
    {
        _email = email;
        _opts = opts.Value;
    }

    public async Task SubmitAsync(AmAssignmentChangeRequestModel model, string fromUserEmail, CancellationToken ct = default)
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
            var subject = $"[AM-ACR {submissionId}] AM Assignment Change Request (Open Contracts) - {model.NewAmName}";
            var body = BuildHtmlBody(model, openLines,
                groupTitle: "Accounts WITH Assign Open Contracts checked",
                submittedBy: fromUserEmail,
                submissionId: submissionId);

            await _email.SendEmailHtmlAsyncStrict(_opts.OpenContractsTo, subject, body, _opts.OpenContractsCc, ct);
        }

        if (standardLines.Count > 0)
        {
            var subject = $"[AM-ACR {submissionId}] AM Assignment Change Request - {model.NewAmName}";
            var body = BuildHtmlBody(model, standardLines,
                groupTitle: "Accounts WITHOUT Assign Open Contracts checked",
                submittedBy: fromUserEmail,
                submissionId: submissionId);

            await _email.SendEmailHtmlAsyncStrict(_opts.StandardTo, subject, body, ccEmail: null, ct);
        }
    }

    private static string BuildHtmlBody(
        AmAssignmentChangeRequestModel m,
        List<AmAssignmentCustomerLine> lines,
        string groupTitle,
        string submittedBy,
        string submissionId)
    {
        static string E(string s) => WebUtility.HtmlEncode(s ?? "");

        var sb = new StringBuilder();
        sb.AppendLine("<div style='font-family:Segoe UI, Arial, sans-serif; font-size:14px;'>");

        sb.AppendLine("<h2 style='margin:0 0 12px 0;'>AM Assignment Change Request</h2>");
        sb.AppendLine($"<div style='margin-bottom:12px; color:#666;'><b>Submission ID:</b> {E(submissionId)} &nbsp; | &nbsp; <b>Submitted By:</b> {E(submittedBy)}</div>");

        sb.AppendLine("<div style='margin-bottom:12px;'>");
        sb.AppendLine($"<div><b>Executive/DM/DSM Name:</b> {E(m.ExecutiveName)}</div>");
        sb.AppendLine($"<div><b>Location:</b> {E(m.Location)}</div>");
        sb.AppendLine($"<div><b>Salesperson Type:</b> {E(m.SalespersonType)}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div style='margin-bottom:12px;'>");
        sb.AppendLine("<div style='margin-bottom:6px;'><b>Current Assigned AM</b></div>");
        sb.AppendLine($"<div><b>Name:</b> {E(m.CurrentAmName)}</div>");
        sb.AppendLine($"<div><b>Sales Rep #:</b> {E(m.CurrentAmSalesRepNumber)}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div style='margin-bottom:12px;'>");
        sb.AppendLine("<div style='margin-bottom:6px;'><b>New Assigned AM</b></div>");
        sb.AppendLine($"<div><b>Name:</b> {E(m.NewAmName)}</div>");
        sb.AppendLine($"<div><b>Sales Rep #:</b> {E(m.NewAmSalesRepNumber)}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<h3 style='margin:18px 0 8px 0;'>{E(groupTitle)}</h3>");

        sb.AppendLine("<table style='border-collapse:collapse; width:100%; max-width:900px;'>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left; width:160px;'>Acct#</th>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left;'>Company Name</th>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left; width:190px;'>Assign Open Contracts</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var a in lines)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(a.AccountNumber)}</td>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(a.CompanyName)}</td>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{(a.AssignOpenContracts ? "Yes" : "No")}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div style='margin-top:16px; color:#666;'>");
        sb.AppendLine("Note: Forms are only approved if submitted directly from Executive/DM/DSM email.");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        return sb.ToString();
    }
}
