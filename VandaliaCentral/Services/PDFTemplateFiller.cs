using System.IO;

using iTextSharp.text.pdf;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class PDFTemplateFiller
    {
        public static byte[] FillTermination(TerminationFormModel model)
        {
            var templatePath = Path.Combine("wwwroot", "forms", "EmployeeTerminationTemplate.pdf");
            using var reader = new PdfReader(templatePath);
            using var output = new MemoryStream();
            using var stamper = new PdfStamper(reader, output);

            var fields = stamper.AcroFields;

            fields.SetField("EmployeeName", model.EmployeeName);
            fields.SetField("EmployeeNumber", model.EmployeeNumber);
            fields.SetField("Location", model.Location);
            fields.SetField("LastDateOfEmployment", model.LastDateOfEmployment.ToString("MM/dd/yyyy"));
            fields.SetField("Cellphone", model.Cellphone ? "Yes" : "Off");
            fields.SetField("Notepad", model.Notepad ? "Yes" : "Off");
            fields.SetField("DeviceAccounts", model.DeviceAccounts ? "Yes" : "Off");
            fields.SetField("Uniforms", model.Uniforms ? "Yes" : "Off");
            fields.SetField("CompanyVehicle", model.CompanyVehicle ? "Yes" : "Off");
            fields.SetField("CreditCard", model.CreditCard ? "Yes" : "Off");
            fields.SetField("Laptop", model.Laptop ? "Yes" : "Off");
            fields.SetField("Hotspot", model.Hotspot ? "Yes" : "Off");
            fields.SetField("DeviceLock", model.DeviceLock ? "Yes" : "Off");
            fields.SetField("Keys", model.Keys ? "Yes" : "Off");
            fields.SetField("PromotionalItems", model.PromotionalItems ? "Yes" : "Off");

            fields.SetField("Microsoft365", model.Microsoft365 ? "Yes" : "Off");
            fields.SetField("Vizion", model.Vizion ? "Yes" : "Off");
            fields.SetField("Salesforce", model.Salesforce ? "Yes" : "Off");
            fields.SetField("Certify", model.Certify ? "Yes" : "Off");
            fields.SetField("RentalMan", model.RentalMan ? "Yes" : "Off");
            fields.SetField("Telematics", model.Telematics ? "Yes" : "Off");
            fields.SetField("Paylocity", model.Paylocity ? "Yes" : "Off");
            fields.SetField("Ninety", model.Ninety ? "Yes" : "Off");
            fields.SetField("Other", model.Other ? "Yes" : "Off");
            fields.SetField("OtherText", model.OtherText);

            // Optional: flatten the form so fields can't be edited after filling
            stamper.FormFlattening = true;

            stamper.Close();
            reader.Close();

            return output.ToArray();
        }


        public static byte[] FillChange(ChangeFormModel model)
        {
            var templatePath = Path.Combine("wwwroot", "forms", "EmployeeChangeTemplate.pdf");
            using var reader = new PdfReader(templatePath);
            using var output = new MemoryStream();
            using var stamper = new PdfStamper(reader, output);

            var fields = stamper.AcroFields;

            fields.SetField("FirstName", model.FirstName);
            fields.SetField("LastName", model.LastName);
            fields.SetField("PreviousJobTitle", model.PreviousJobTitle);
            fields.SetField("BranchNumber", model.BranchNumber);
            fields.SetField("BranchName", model.BranchName);
            fields.SetField("ManagerName", model.ManagerName);
            fields.SetField("EffectiveDate", model.EffectiveDate.ToString("MM/dd/yyyy"));
            fields.SetField("NewPosition", model.NewPosition);
            fields.SetField("JobTitle", model.JobTitle);
            fields.SetField("FullTime", model.FullTime ? "Yes" : "Off");
            fields.SetField("PartTime", model.PartTime ? "Yes" : "Off");
            fields.SetField("Hourly", model.Hourly ? "Yes" : "Off");
            fields.SetField("Salary", model.Salary ? "Yes" : "Off");
            fields.SetField("NewLocBranchNumber", model.NewLocBranchNumber);
            fields.SetField("NewLocBranchName", model.NewLocBranchName);
            fields.SetField("NewLocManagerName", model.NewLocManagerName);

            fields.SetField("AdditionalNotes", model.AdditionalNotes);
            

            // Optional: flatten the form so fields can't be edited after filling
            stamper.FormFlattening = true;

            stamper.Close();
            reader.Close();

            return output.ToArray();
        }
    }
}
