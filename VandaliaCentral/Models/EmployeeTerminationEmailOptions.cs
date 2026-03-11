namespace VandaliaCentral.Models;

public sealed class EmployeeTerminationEmailOptions
{
    public string To { get; set; } = "";     // required
    public string? Cc { get; set; }           // optional
}

