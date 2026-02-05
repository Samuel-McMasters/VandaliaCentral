namespace VandaliaCentral.Services;

public interface IFreshserviceService
{
    Task<FreshserviceCreateTicketResult> CreateTicketAsync(FreshserviceCreateTicketInput input, CancellationToken ct = default);
}

public sealed class FreshserviceCreateTicketInput
{
    public required string RequesterEmail { get; init; }
    public required string Subject { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public string? SubCategory { get; init; }

    public required string PriorityLabel { get; init; }
}


public sealed class FreshserviceCreateTicketResult
{
    public required long TicketId { get; init; }
}

public sealed class FreshserviceFieldError
{
    public required string Field { get; init; }
    public required string Message { get; init; }
}
