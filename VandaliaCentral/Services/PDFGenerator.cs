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
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Employee Termination Form").FontSize(20).Bold().Underline().AlignCenter();
                        col.Item().Text($"Employee Name: {model.EmployeeName}");
                        col.Item().Text($"Termination Date: {model.TerminationDate:MMMM dd, yyyy}");
                        col.Item().Text($"Reason:\n{model.Reason}");
                    });
                });

            }).GeneratePdf();
        }
    }
}
