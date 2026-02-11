using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;

namespace VandaliaCentral.Services
{
    public class GraphEmailService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly ILogger<GraphEmailService> _logger;

        public GraphEmailService(
            ITokenAcquisition tokenAcquisition,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            ILogger<GraphEmailService> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _consentHandler = consentHandler;
            _logger = logger;
        }

        // =========================
        // Existing methods (legacy)
        // =========================

        public async Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string bodyText,
            byte[] pdfBytes,
            string pdfFileName)
        {
            try
            {
                var graphClient = await GetGraphClientAsync();

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = bodyText
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = toEmail }
                        }
                    },
                    Attachments = new MessageAttachmentsCollectionPage
                    {
                        new FileAttachment
                        {
                            Name = pdfFileName,
                            ContentBytes = pdfBytes,
                            ContentType = "application/pdf"
                        }
                    }
                };

                await graphClient.Me.SendMail(message, true)
                    .Request()
                    .PostAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailWithAttachmentAsync failed. To={To}, Subject={Subject}, File={FileName}",
                    toEmail, subject, pdfFileName);

                _consentHandler.HandleException(ex);
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyText)
        {
            try
            {
                var graphClient = await GetGraphClientAsync();

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = bodyText
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = toEmail }
                        }
                    }
                };

                await graphClient.Me.SendMail(message, true)
                    .Request()
                    .PostAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailAsync failed. To={To}, Subject={Subject}", toEmail, subject);
                _consentHandler.HandleException(ex);
            }
        }

        public async Task SendEmailHtmlAsync(string toEmail, string subject, string bodyHtml, string? ccEmail = null)
        {
            try
            {
                var graphClient = await GetGraphClientAsync();

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = bodyHtml
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = toEmail }
                        }
                    }
                };

                if (!string.IsNullOrWhiteSpace(ccEmail))
                {
                    message.CcRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = ccEmail }
                        }
                    };
                }

                await graphClient.Me.SendMail(message, true)
                    .Request()
                    .PostAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailHtmlAsync failed. To={To}, Subject={Subject}, Cc={Cc}",
                    toEmail, subject, ccEmail);

                _consentHandler.HandleException(ex);
            }
        }

        public async Task EnsureMailSendAccessAsync()
        {
            try
            {
                _ = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Mail.Send" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnsureMailSendAccessAsync failed.");
                _consentHandler.HandleException(ex);
            }
        }

        // =========================
        // STRICT methods (new)
        // These THROW on failure so UI can show an error.
        // =========================

        public async Task SendEmailHtmlAsyncStrict(
            string toEmail,
            string subject,
            string bodyHtml,
            string? ccEmail = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("Destination email is blank.");

            try
            {
                var graphClient = await GetGraphClientAsync(ct);

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = bodyHtml
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = toEmail }
                        }
                    }
                };

                if (!string.IsNullOrWhiteSpace(ccEmail))
                {
                    message.CcRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = ccEmail }
                        }
                    };
                }

                await ExecuteWithRetryAsync(async () =>
                {
                    await graphClient.Me.SendMail(message, true)
                        .Request()
                        .PostAsync(ct);
                }, ct);
            }
            catch (Exception ex)
            {
                // Log the FULL exception text
                _logger.LogError(ex, "SendEmailHtmlAsyncStrict failed. To={To}, Subject={Subject}, Cc={Cc}",
                    toEmail, subject, ccEmail);

                // Keep consent/CA behavior
                _consentHandler.HandleException(ex);

                // IMPORTANT: Bubble failure up so your page shows an error
                throw;
            }
        }

        public async Task SendEmailTextAsyncStrict(
            string toEmail,
            string subject,
            string bodyText,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("Destination email is blank.");

            try
            {
                var graphClient = await GetGraphClientAsync(ct);

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = bodyText
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = toEmail }
                        }
                    }
                };

                await ExecuteWithRetryAsync(async () =>
                {
                    await graphClient.Me.SendMail(message, true)
                        .Request()
                        .PostAsync(ct);
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailTextAsyncStrict failed. To={To}, Subject={Subject}", toEmail, subject);
                _consentHandler.HandleException(ex);
                throw;
            }
        }

        // =========================
        // Internals
        // =========================

        private async Task<GraphServiceClient> GetGraphClientAsync(CancellationToken ct = default)
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Mail.Send" });
            return new GraphServiceClient(new DelegateAuthenticationProvider(request =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return Task.CompletedTask;
            }));
        }

        private static async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken ct)
        {
            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (ServiceException ex) when (attempt < maxAttempts && IsTransient(ex))
                {
                    var delay = GetRetryDelay(ex, attempt);
                    await Task.Delay(delay, ct);
                }
            }

            // Last attempt: let it throw for real
            await action();
        }

        private static bool IsTransient(ServiceException ex)
        {
            return ex.StatusCode == HttpStatusCode.TooManyRequests
                || ex.StatusCode == HttpStatusCode.ServiceUnavailable
                || ex.StatusCode == HttpStatusCode.GatewayTimeout
                || ex.StatusCode == HttpStatusCode.BadGateway;
        }

        private static TimeSpan GetRetryDelay(ServiceException ex, int attempt)
        {
            try
            {
                // Some Graph SDK versions expose Retry-After in ResponseHeaders.
                if (ex.ResponseHeaders != null &&
                    ex.ResponseHeaders.TryGetValues("Retry-After", out var values))
                {
                    var raw = values?.FirstOrDefault();
                    if (int.TryParse(raw, out var seconds) && seconds > 0)
                        return TimeSpan.FromSeconds(seconds);
                }

            }
            catch
            {
                // ignore header parsing errors
            }

            // Simple backoff
            return TimeSpan.FromSeconds(2 * attempt);
        }
    }
}
