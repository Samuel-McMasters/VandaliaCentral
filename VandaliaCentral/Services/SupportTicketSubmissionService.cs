using System.Text;

namespace VandaliaCentral.Services;

public interface ISupportTicketSubmissionService
{
    Task<SupportTicketSubmissionResult> SubmitAsync(FreshserviceCreateTicketInput input, CancellationToken ct = default);
}

public sealed class SupportTicketSubmissionResult
{
    public bool CreatedInFreshservice { get; init; }
    public long? FreshserviceTicketId { get; init; }
    public bool SentFallbackEmail { get; init; }
}

public sealed class SupportTicketSubmissionService : ISupportTicketSubmissionService
{
    private readonly IFreshserviceService _freshservice;
    private readonly GraphEmailService _email;

    private const string SupportEmail = "support@vandaliarental.com";
    private const string DevCcEmail = "sam.mcmasters@vandaliarental.com"; // “CC” copy target

    public SupportTicketSubmissionService(IFreshserviceService freshservice, GraphEmailService email)
    {
        _freshservice = freshservice;
        _email = email;
    }

    public async Task<SupportTicketSubmissionResult> SubmitAsync(
        FreshserviceCreateTicketInput input,
        CancellationToken ct = default)
    {
        try
        {
            var created = await _freshservice.CreateTicketAsync(input, ct);

            return new SupportTicketSubmissionResult
            {
                CreatedInFreshservice = true,
                FreshserviceTicketId = created.TicketId,
                SentFallbackEmail = false
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // If the user navigates away / request is cancelled, do NOT email Support.
            throw;
        }
        catch (Exception freshserviceEx)
        {
            // ANY Freshservice failure => send fallback email
            // Requirement: email subject should be the ticket subject.
            var emailSubject = input.Subject;
            var emailBody = BuildFallbackEmailBody(input, freshserviceEx);

            try
            {
                // 1) Send to Support
                await _email.SendEmailAsync(
                    toEmail: SupportEmail,
                    subject: emailSubject,
                    bodyText: emailBody
                );

                // 2) Send a “CC copy” to Sam (so you’re always aware)
                await _email.SendEmailAsync(
                    toEmail: DevCcEmail,
                    subject: emailSubject,
                    bodyText: "[COPY] This fallback ticket email was sent to Support.\n\n" + emailBody
                );
            }
            catch (Exception emailEx)
            {
                // If even the fallback email fails, bubble up so UI shows a real failure
                throw new SupportTicketSubmissionException(
                    $"Freshservice failed AND fallback email failed. Freshservice error: {freshserviceEx.Message} | Email error: {emailEx.Message}",
                    freshserviceEx,
                    emailEx
                );
            }

            return new SupportTicketSubmissionResult
            {
                CreatedInFreshservice = false,
                FreshserviceTicketId = null,
                SentFallbackEmail = true
            };
        }
    }

    private static string BuildFallbackEmailBody(FreshserviceCreateTicketInput input, Exception freshserviceEx)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Freshservice API error. Ticket submission was emailed instead.");
        sb.AppendLine("Please provide this message to development for review.");
        sb.AppendLine();

        sb.AppendLine($"Error: {freshserviceEx.Message}");
        sb.AppendLine();

        sb.AppendLine("Ticket Details:");
        sb.AppendLine($"Requested By: {input.RequesterEmail}");
        sb.AppendLine($"Subject: {input.Subject}");
        sb.AppendLine($"Category: {input.Category}");
        sb.AppendLine($"Subcategory: {(string.IsNullOrWhiteSpace(input.SubCategory) ? "(none)" : input.SubCategory)}");

        // input.PriorityLabel is your stable key (LOW/HIGH/CRITICAL)
        var priorityKey = (input.PriorityLabel ?? "").Trim().ToUpperInvariant();
        sb.AppendLine($"Priority: {PriorityKeyToLabel(priorityKey)} (key: {priorityKey})");

        sb.AppendLine();
        sb.AppendLine("Details:");
        sb.AppendLine(input.Description);

        sb.AppendLine();
        sb.AppendLine("----");
        sb.AppendLine("Technical Details (for development):");
        sb.AppendLine(freshserviceEx.ToString());

        return sb.ToString();
    }

    private static string PriorityKeyToLabel(string priorityKey)
    {
        return priorityKey switch
        {
            "LOW" => "Low-No due date",
            "HIGH" => "High-I can operate but need help",
            "CRITICAL" => "Critical-I am Down",
            _ => priorityKey
        };
    }
}

public sealed class SupportTicketSubmissionException : Exception
{
    public Exception FreshserviceException { get; }
    public Exception EmailException { get; }

    public SupportTicketSubmissionException(string message, Exception freshserviceException, Exception emailException)
        : base(message, emailException)
    {
        FreshserviceException = freshserviceException;
        EmailException = emailException;
    }
}
