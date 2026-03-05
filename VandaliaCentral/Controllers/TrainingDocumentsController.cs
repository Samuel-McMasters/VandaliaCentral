using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VandaliaCentral.Services;

namespace VandaliaCentral.Controllers;

[ApiController]
[Authorize]
[Route("api/training-documents")]
public class TrainingDocumentsController : ControllerBase
{
    private readonly TrainingDocumentService _trainingDocumentService;

    public TrainingDocumentsController(TrainingDocumentService trainingDocumentService)
    {
        _trainingDocumentService = trainingDocumentService;
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string fileName)
    {
        var download = await _trainingDocumentService.DownloadDocumentAsync(fileName);
        if (download == null)
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(download.Details.ContentType)
            ? "application/octet-stream"
            : download.Details.ContentType;

        return File(download.Content, contentType, Path.GetFileName(fileName));
    }
}
