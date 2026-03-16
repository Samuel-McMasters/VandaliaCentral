using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VandaliaCentral.Models;
using VandaliaCentral.Services;

namespace VandaliaCentral.Controllers;

[ApiController]
[Authorize]
[Route("api/it-project-tracker")]
public class ItProjectTrackerController : ControllerBase
{
    private const string AdminGroupId = "1f7897c7-a5b7-437c-9697-626c1e758f04";
    private readonly ItProjectTrackerService _itProjectTrackerService;

    public ItProjectTrackerController(ItProjectTrackerService itProjectTrackerService)
    {
        _itProjectTrackerService = itProjectTrackerService;
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download()
    {
        var isAdmin = User.Claims.Any(c => c.Type == "groups" && c.Value == AdminGroupId);
        if (!isAdmin)
        {
            return Forbid();
        }

        var userName = User?.Identity?.Name ?? "anonymous";
        var items = await _itProjectTrackerService.LoadItemsAsync(userName);
        var generatedOn = DateTime.Now;

        var pdfBytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("IT Project Tracker Report").SemiBold().FontSize(18);
                        column.Item().Text($"User: {userName}");
                        column.Item().Text($"Generated: {generatedOn:MMM d, yyyy h:mm tt}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    if (!items.Any())
                    {
                        column.Item().Text("No project entries were found for this user.").FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    foreach (var item in items)
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(itemColumn =>
                        {
                            itemColumn.Item().Text(item.Title).SemiBold().FontSize(13);
                            itemColumn.Item().Text($"Completed: {FormatCompletedDate(item)}").FontColor(Colors.Grey.Darken1);

                            if (!string.IsNullOrWhiteSpace(item.Notes))
                            {
                                itemColumn.Item().PaddingTop(4).Text(item.Notes);
                            }
                            else
                            {
                                itemColumn.Item().PaddingTop(4).Text("No notes provided.").Italic().FontColor(Colors.Grey.Darken1);
                            }
                        });

                        column.Item().PaddingBottom(8);
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        var safeName = SanitizeUserNameForFile(userName);
        var fileName = $"it-project-tracker-{safeName}-{generatedOn:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static string FormatCompletedDate(ItProjectTrackerItem item)
    {
        return item.CompletedDate.HasValue
            ? item.CompletedDate.Value.ToString("MMM d, yyyy")
            : "Not set";
    }

    private static string SanitizeUserNameForFile(string userName)
    {
        var localPart = userName.Split('@')[0];
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(localPart.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "user" : sanitized;
    }
}
