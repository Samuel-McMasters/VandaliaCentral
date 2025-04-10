using Microsoft.Graph;

namespace VandaliaCentral.Services
{
    public class EmailsService
    {
        private readonly GraphServiceClient _graphClient;

        public EmailsService(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
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
                            Address = "sam.mcmasters@vandaliarental.com" // Replace later if needed
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
    }
}
