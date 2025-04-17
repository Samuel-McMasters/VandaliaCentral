using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Client;

namespace VandaliaCentral.Services
{
    public class GraphEmailService
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public GraphEmailService(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        public async Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string bodyText,
            byte[] pdfBytes,
            string pdfFileName,
            NavigationManager navigation)
        {
            try
            {
                var graphClient = await GetGraphClientAsync(navigation);

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
            catch (MsalUiRequiredException)
            {
                navigation.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyText, NavigationManager navigation)
        {
            try
            {
                var graphClient = await GetGraphClientAsync(navigation);

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
            catch (MsalUiRequiredException)
            {
                navigation.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
            }
        }

        private async Task<GraphServiceClient> GetGraphClientAsync(NavigationManager navigation)
        {
            try
            {
                var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Mail.Send" });
                return new GraphServiceClient(new DelegateAuthenticationProvider(request =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return Task.CompletedTask;
                }));
            }
            catch (MsalUiRequiredException)
            {
                navigation.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
                throw; // Still throw to avoid further execution
            }
        }
    }
}