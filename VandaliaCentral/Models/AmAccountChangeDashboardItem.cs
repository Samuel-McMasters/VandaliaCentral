namespace VandaliaCentral.Models;

public sealed class AmAccountChangeDashboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SubmissionId { get; set; } = "";
    public string SubmittedByEmail { get; set; } = "";
    public string SubmittedByName { get; set; } = "";
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string ExecutiveName { get; set; } = "";
    public string Location { get; set; } = "";
    public string SalespersonType { get; set; } = "";
    public string CurrentAmName { get; set; } = "";
    public string CurrentAmSalesRepNumber { get; set; } = "";
    public string NewAmName { get; set; } = "";
    public string NewAmSalesRepNumber { get; set; } = "";

    public string AccountNumber { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public bool AssignOpenContracts { get; set; }
    public bool ReferralAccount { get; set; }
}
