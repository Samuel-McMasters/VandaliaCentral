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
            var templatePath = Path.Combine("wwwroot", "forms", "EmployeeChangeCrossTrainTemplate.pdf");
            using var reader = new PdfReader(templatePath);
            using var output = new MemoryStream();
            using var stamper = new PdfStamper(reader, output);

            var fields = stamper.AcroFields;

            fields.SetField("first_name", model.FirstName);
            fields.SetField("last_name", model.LastName);
            // Support both known field-name spellings seen across template revisions.
            fields.SetField("preivous_job_title", model.PreviousJobTitle);
            fields.SetField("previous_job_title", model.PreviousJobTitle);
            fields.SetField("PreviousJobTitle", model.PreviousJobTitle);
            fields.SetField("branch_number", model.BranchNumber);
            fields.SetField("branch_name", model.BranchName);
            fields.SetField("manager_name", model.ManagerName);
            fields.SetField("effective_date", model.EffectiveDate.ToString("MM/dd/yyyy"));
            fields.SetField("new_position", model.NewPosition);
            fields.SetField("job_title", model.JobTitle);
            fields.SetField("full_time", model.FullTime ? "Yes" : "Off");
            fields.SetField("part_time", model.PartTime ? "Yes" : "Off");
            fields.SetField("hourly", model.Hourly ? "Yes" : "Off");
            fields.SetField("salary", model.Salary ? "Yes" : "Off");
            fields.SetField("new_branch_number", model.NewLocBranchNumber);
            fields.SetField("new_branch_name", model.NewLocBranchName);
            fields.SetField("new_manager_name", model.NewLocManagerName);
            fields.SetField("cross_training_current_position", model.CrossTrainingCurrentPosition);
            fields.SetField("cross_training_target_position", model.CrossTrainingTargetPosition);
            fields.SetField("additional_notes", model.AdditionalNotes);
            

            // Optional: flatten the form so fields can't be edited after filling
            stamper.FormFlattening = true;

            stamper.Close();
            reader.Close();

            return output.ToArray();
        }
    }
}
