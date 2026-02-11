namespace VandaliaCentral.Models;

public sealed class EmployeeChangeEmailOptions
{
    public string To { get; set; } = "";     // required
    public string? Cc { get; set; }          // optional
}
