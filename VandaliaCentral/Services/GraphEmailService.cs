using Microsoft.Graph;
using Microsoft.Identity.Web;

using System.Net.Http.Headers;

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
            string pdfFileName)
        {
            var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
            {
                var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Mail.Send" });
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }));

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
    }
}