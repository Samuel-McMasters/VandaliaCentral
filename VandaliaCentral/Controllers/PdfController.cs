using System.IO;

using Microsoft.AspNetCore.Mvc;

namespace VandaliaCentral.Controllers
{
    [Route("api/pdfs")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        

        private readonly string bcFolder = @"E:\SharedFolders\Company\MSAccess\BE\GL\Bonus2025";
        private readonly string mmFolder = @"E:\SharedFolders\Company\IT\Intranet\mm";



        [HttpGet("latest-bonus-chart")]
        public IActionResult GetLatestBonusChart()
        {
            if (!Directory.Exists(bcFolder))
            {
                return NotFound("PDF folder not found");
            }

            var files = Directory.GetFiles(bcFolder, "*.pdf");
            var latestFile = files.Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            if (latestFile == null)
            {
                return NotFound("No PDF files found.");
            }

            var fileStream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read);
            var result = new FileStreamResult(fileStream, "application/pdf");
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{latestFile.Name}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return new FileStreamResult(fileStream, "application/pdf");

        }


        [HttpGet("latest-monday-minute")]
        public IActionResult GetLatestMondayMinute()
        {
            if (!Directory.Exists(mmFolder))
            {
                return NotFound("PDF folder not found");
            }

            var files = Directory.GetFiles(mmFolder, "*.pdf");
            var latestFile = files.Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            if (latestFile == null)
            {
                return NotFound("No PDF files found.");
            }

            var fileStream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read);
            var result = new FileStreamResult(fileStream, "application/pdf");
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{latestFile.Name}\"";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return new FileStreamResult(fileStream, "application/pdf");

        }

        //Endpoint for dynamically naming Monday Minute header
        [HttpGet("latest-monday-minute-title")]
        public IActionResult GetLatestMondayMinuteTitle()
        {
            if (!Directory.Exists(mmFolder))
            {
                return NotFound("PDF folder not found");
            }

            var files = Directory.GetFiles(mmFolder, "*.pdf");
            var latestFile = files
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latestFile == null)
            {
                return NotFound("No PDF files found.");
            }

            var title = Path.GetFileNameWithoutExtension(latestFile.Name);
            return Ok(title);
        }
    }
}
