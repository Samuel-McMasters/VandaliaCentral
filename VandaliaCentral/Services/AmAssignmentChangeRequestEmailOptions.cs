namespace VandaliaCentral.Services;

public sealed class AmAssignmentChangeRequestEmailOptions
{
    // TODO: Set real email for lines where AssignOpenContracts = true
    public string OpenContractsTo { get; set; } = "";

    // TODO: Set real CC for lines where AssignOpenContracts = true
    public string OpenContractsCc { get; set; } = "";

    // TODO: Set real email for lines where AssignOpenContracts = false
    public string StandardTo { get; set; } = "";
}
