using System.ComponentModel.DataAnnotations;

namespace VandaliaCentral.Models;

public sealed class AmAssignmentChangeRequestModel
{
    [Required] public string ExecutiveName { get; set; } = "";
    [Required] public string Location { get; set; } = "";
    [Required] public string SalespersonType { get; set; } = ""; // "General Rental" or "SOS"

    [Required] public string CurrentAmName { get; set; } = "";
    [Required] public string CurrentAmSalesRepNumber { get; set; } = "";

    [Required] public string NewAmName { get; set; } = "";
    [Required] public string NewAmSalesRepNumber { get; set; } = "";

    public List<AmAssignmentCustomerLine> Accounts { get; set; } = new()
    {
        new AmAssignmentCustomerLine()
    };
}

public sealed class AmAssignmentCustomerLine
{
    public string AccountNumber { get; set; } = "";
    public string CompanyName { get; set; } = "";

    // NEW
    public bool AssignOpenContracts { get; set; }
}
