namespace VandaliaCentral.Services;

public sealed class FreshserviceOptions
{
    public string Domain { get; set; } = "vandaliarental.freshservice.com";
    public string ApiKey { get; set; } = "";               // store in Azure App Service settings (NOT in git)
    public long DepartmentId { get; set; }                 // 16000022810
    public long ResponderId { get; set; }                  // 16001056258
}

