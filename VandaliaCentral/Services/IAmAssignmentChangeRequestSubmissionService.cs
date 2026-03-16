using VandaliaCentral.Models;

namespace VandaliaCentral.Services;

public interface IAmAssignmentChangeRequestSubmissionService
{
    Task SubmitAsync(AmAssignmentChangeRequestModel model, string fromUserEmail, string submittedByName, CancellationToken ct = default);
}
