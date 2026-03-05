using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VandaliaCentral.Services;

namespace VandaliaCentral.Controllers;

[ApiController]
[Authorize]
[Route("api/sow-documents")]
public class SowDocumentsController : ControllerBase
{
    private readonly SowDocumentService _sowDocumentService;

    public SowDocumentsController(SowDocumentService sowDocumentService)
    {
        _sowDocumentService = sowDocumentService;
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string fileName)
    {
        var download = await _sowDocumentService.DownloadDocumentAsync(fileName);
        if (download == null)
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(download.Details.ContentType)
            ? "application/pdf"
            : download.Details.ContentType;

        return File(download.Content, contentType, Path.GetFileName(fileName));
    }
}
