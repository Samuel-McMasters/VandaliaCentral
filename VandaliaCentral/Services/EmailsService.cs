using Microsoft.Graph;

namespace VandaliaCentral.Services
{
    public class EmailsService
    {
        

        private readonly GraphServiceClient _graphClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailsService(GraphServiceClient graphClient, IHttpContextAccessor httpContextAccessor)
        {
            _graphClient = graphClient;
            _httpContextAccessor = httpContextAccessor;
        }


       

        public async Task SendTerminationFormAsync(byte[] pdfBytes, string employeeName)
        {
            var message = new Message
            {
                Subject = $"Termination Form: {employeeName}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = $"Attached is the termination form for {employeeName}."
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = "sam.mcmasters@vandaliarental.com" // TODO: Update this email
                        }
                    }
                },
                Attachments = new MessageAttachmentsCollectionPage
                {
                    new FileAttachment
                    {
                        Name = $"{employeeName}_termination.pdf",
                        ContentBytes = pdfBytes,
                        ContentType = "application/pdf"
                    }
                }
            };

            await _graphClient.Me.SendMail(
                new Message
                {
                    Subject = message.Subject,
                    Body = message.Body,
                    ToRecipients = message.ToRecipients,
                    Attachments = message.Attachments
                },
                true // SaveToSentItems
            ).Request().PostAsync();
        }

        public async Task SendChangeFormAsync(byte[] pdfBytes, string employeeName)
        {
            var message = new Message
            {
                Subject = $"Employee Change Form: {employeeName}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = $"Attached is the Employee Change form for {employeeName}."
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = "sam.mcmasters@vandaliarental.com" // TODO: Update this email
                        }
                    }
                },
                Attachments = new MessageAttachmentsCollectionPage
                {
                    new FileAttachment
                    {
                        Name = $"{employeeName}_change.pdf",
                        ContentBytes = pdfBytes,
                        ContentType = "application/pdf"
                    }
                }
            };

            await _graphClient.Me.SendMail(
                new Message
                {
                    Subject = message.Subject,
                    Body = message.Body,
                    ToRecipients = message.ToRecipients,
                    Attachments = message.Attachments
                },
                true // SaveToSentItems
            ).Request().PostAsync();
        }
    }
}
