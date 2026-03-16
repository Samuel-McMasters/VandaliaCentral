using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public interface IAmAccountChangeDashboardService
{
    Task QueueOpenContractAccountsAsync(AmAssignmentChangeRequestModel model, IEnumerable<AmAssignmentCustomerLine> lines, string submittedByEmail, string submittedByName, string submissionId, CancellationToken ct = default);
    IReadOnlyList<AmAccountChangeDashboardItem> GetPending();
    int GetPendingCount();
    Task ApproveAsync(string itemId, string approvedBy, CancellationToken ct = default);
    Task DenyAsync(string itemId, string deniedBy, CancellationToken ct = default);
}
