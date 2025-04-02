using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;

namespace VandaliaCentral.Services
{
    public class EmailService
    {
        private readonly string smtpServer = "smtp.office365.com";
        private readonly int smtpPort = 587;

        //TODO --> Update this email
        private readonly string fromEmail = "sam.mcmasters@vandaliarental.com";

        //TODO --> Add a distrogroup
        private readonly string distroGroup = "trevor.mackey@vandaliarental.com ";

        public async Task SendTerminationFormAsync(byte[] pdfBytes, string employeeName)
        {
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential("sam.mcmasters@vandaliarental.com", "Yosemite2023Zion2021!"),
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
