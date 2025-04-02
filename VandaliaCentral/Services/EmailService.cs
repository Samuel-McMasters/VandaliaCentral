using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;

namespace VandaliaCentral.Services
{
    public class EmailService
    {
        private readonly string smtpServer = "smtp.vandaliarental.com";
        private readonly int smtpPort = 587;
        private readonly string fromEmail = "noreply@vandaliarental.com";

        //TODO --> Add a distrogroup
        private readonly string distroGroup = "";

        public async Task SendTerminationFormAsync(byte[] pdfBytes, string employeeName)
        {
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential("your-smtp-user", "your-smtp-pass"),
                EnableSsl = true
            };

            var mail = new MailMessage(fromEmail, distroGroup)
            {
                Subject = $"Termination Form: {employeeName}",
                Body = $"Attached is the termination form for {employeeName}"
            };

            mail.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"{employeeName}_termination.pdf"));

            await client.SendMailAsync(mail);
        }
    }
}
