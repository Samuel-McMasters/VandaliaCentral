namespace VandaliaCentral.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminGroupId = "1f7897c7-a5b7-437c-9697-626c1e758f04";
    public const string VandaliaCentralAmDashboardGroupId = "b906c024-ffca-4201-b57e-71223270cca5";

    public static readonly HashSet<string> HrFormsAllowedGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        AdminGroupId,
        "cf596cae-8bfa-4518-b5c4-2877d0ad8469",
        "10676280-2877-4133-b8e9-d3c4dc090143",
        "132c320f-a891-4dd6-8367-5967fd7e0b4f",
        "368d85a2-0c38-46a2-9c8a-a6539a933107",
        "258d799b-8ba1-4507-89a4-7006981a37b9"
    };

    public static readonly HashSet<string> AmAccountChangeAllowedGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        AdminGroupId,
        VandaliaCentralAmDashboardGroupId,
        "132c320f-a891-4dd6-8367-5967fd7e0b4f",
        "368d85a2-0c38-46a2-9c8a-a6539a933107",
        "258d799b-8ba1-4507-89a4-7006981a37b9"
    };

    public const string MondayMinuteUploaderGroupId = "b3a0f732-a5f2-4c28-a005-61c901b21096";

    public static readonly HashSet<string> AccountChangeDashboardAllowedGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        AdminGroupId,
        VandaliaCentralAmDashboardGroupId,
        MondayMinuteUploaderGroupId
    };

    public const string HrFormsAccess = nameof(HrFormsAccess);
    public const string AmAccountChangeAccess = nameof(AmAccountChangeAccess);
    public const string AccountChangeDashboardAccess = nameof(AccountChangeDashboardAccess);
}
