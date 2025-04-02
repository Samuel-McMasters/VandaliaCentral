using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using VandaliaCentral.Models;

namespace VandaliaCentral.Services
{
    public class PDFGenerator
    {
        public static byte[] Generate(TerminationFormModel model)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.Content().Text(text =>
                    {
                        text.Span("Employee Termination Form\n").FontSize(18).Bold();
                        text.Span($"Name: {model.EmployeeName}\n");
                        text.Span($"Date: {model.LastDateOfEmployment.ToShortDateString()}\n");
                        //text.Span($"Reason: {model.Reason}");
                    });
                });
            }).GeneratePdf();
        }
    }
}
