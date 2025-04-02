using System.IO;

using iTextSharp.text.pdf;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class PDFTemplateFiller
    {
        public static byte[] Fill(TerminationFormModel model)
        {
            var templatePath = Path.Combine("wwwroot", "forms", "EmployeeTerminationTemplate.pdf");
            using var reader = new PdfReader(templatePath);
            using var output = new MemoryStream();
            using var stamper = new PdfStamper(reader, output);

            var fields = stamper.AcroFields;

            fields.SetField("EmployeeName", model.EmployeeName);
            fields.SetField("LastDateOfEmployment", model.TerminationDate.ToString("MM/dd/yyyy"));
            //fields.SetField("Reason", model.Reason);

            // Optional: flatten the form so fields can't be edited after filling
            stamper.FormFlattening = true;

            stamper.Close();
            reader.Close();

            return output.ToArray();
        }
    }
}
