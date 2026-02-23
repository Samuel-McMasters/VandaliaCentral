namespace VandaliaCentral.Models;

public sealed class BranchLocationsOptions
{
    public List<BranchLocationItem> Items { get; set; } = new();
}

public sealed class BranchLocationItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
