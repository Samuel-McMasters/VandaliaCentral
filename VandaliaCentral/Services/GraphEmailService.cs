using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace VandaliaCentral.Services
{
    public class GraphEmailService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        public GraphEmailService(
            ITokenAcquisition tokenAcquisition,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _tokenAcquisition = tokenAcquisition;
            _consentHandler = consentHandler;
        }

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
                            EmailAddress = new EmailAddress
                            {
                                Address = toEmail
                            }
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
                            EmailAddress = new EmailAddress
                            {
                                Address = toEmail
                            }
                        }
                    }
                };

                await graphClient.Me.SendMail(message, true)
                    .Request()
                    .PostAsync();
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
            }
        }

        private async Task<GraphServiceClient> GetGraphClientAsync()
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Mail.Send" });
            return new GraphServiceClient(new DelegateAuthenticationProvider(request =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return Task.CompletedTask;
            }));
        }
    }
}