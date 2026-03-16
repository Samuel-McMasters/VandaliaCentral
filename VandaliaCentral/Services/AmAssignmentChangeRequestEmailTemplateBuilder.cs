using System.Net;
using System.Text;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public static class AmAssignmentChangeRequestEmailTemplateBuilder
{
    public static string BuildSubmissionHtmlBody(
        AmAssignmentChangeRequestModel m,
        List<AmAssignmentCustomerLine> lines,
        string groupTitle,
        string submittedBy,
        string submissionId,
        string? approvedBy = null)
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
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left; width:160px;'>Referral Account</th>");

        if (!string.IsNullOrWhiteSpace(approvedBy))
            sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left; width:160px;'>Approved By</th>");

        sb.AppendLine("</tr></thead><tbody>");

        foreach (var a in lines)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(a.AccountNumber)}</td>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(a.CompanyName)}</td>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{(a.AssignOpenContracts ? "Yes" : "No")}</td>");
            sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{(a.ReferralAccount ? "Yes" : "No")}</td>");

            if (!string.IsNullOrWhiteSpace(approvedBy))
                sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(approvedBy)}</td>");

            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div style='margin-top:16px; color:#666;'>");
        sb.AppendLine("Note: Forms are only approved if submitted directly from Executive/DM/DSM email.");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    public static string BuildDeniedHtmlBody(AmAccountChangeDashboardItem item, string deniedBy)
    {
        static string E(string s) => WebUtility.HtmlEncode(s ?? "");

        var sb = new StringBuilder();
        sb.AppendLine("<div style='font-family:Segoe UI, Arial, sans-serif; font-size:14px;'>");
        sb.AppendLine("<h2 style='margin:0 0 12px 0;'>AM Assignment Change Request - Denied</h2>");
        sb.AppendLine($"<div style='margin-bottom:12px; color:#666;'><b>Submission ID:</b> {E(item.SubmissionId)}</div>");
        sb.AppendLine($"<div style='margin-bottom:12px;'><b>Denied By:</b> {E(deniedBy)}</div>");

        sb.AppendLine("<table style='border-collapse:collapse; width:100%; max-width:900px;'>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left;'>Acct#</th>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left;'>Company Name</th>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left;'>Executive/DM/DSM</th>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left;'>Current AM</th>");
        sb.AppendLine("<th style='border:1px solid #ddd; padding:8px; text-align:left;'>New AM</th>");
        sb.AppendLine("</tr></thead><tbody><tr>");
        sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(item.AccountNumber)}</td>");
        sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(item.CompanyName)}</td>");
        sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(item.ExecutiveName)}</td>");
        sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(item.CurrentAmName)} ({E(item.CurrentAmSalesRepNumber)})</td>");
        sb.AppendLine($"<td style='border:1px solid #ddd; padding:8px;'>{E(item.NewAmName)} ({E(item.NewAmSalesRepNumber)})</td>");
        sb.AppendLine("</tr></tbody></table>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }
}
