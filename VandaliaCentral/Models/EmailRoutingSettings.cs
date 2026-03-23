namespace VandaliaCentral.Models;

public sealed class EmailRoutingSettings
{
    public string AmOpenContractsTo { get; set; } = string.Empty;
    public string AmOpenContractsCc { get; set; } = string.Empty;
    public string AmStandardTo { get; set; } = string.Empty;

    public string EmployeeChangeTo { get; set; } = string.Empty;
    public string EmployeeChangeCc { get; set; } = string.Empty;

    public string EmployeeTerminationTo { get; set; } = string.Empty;
    public string EmployeeTerminationCc { get; set; } = string.Empty;

    public string FeedbackTo { get; set; } = string.Empty;

    public string SupportTo { get; set; } = string.Empty;
    public string SupportCc { get; set; } = string.Empty;
}
