using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;


namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MinioFileController : ControllerBase
    {
        private readonly MinioFileService _minioFileService;

        public MinioFileController(MinioFileService minioFileService)
        {
            _minioFileService = minioFileService;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya seçilmedi.");

            var key = await _minioFileService.UploadFileAsync(file);
            return Ok(new { Key = key, Url = _minioFileService.GetFileUrl(key) });
        }

        [HttpGet("download/{*key}")]
        public async Task<IActionResult> Download(string key)
        {
            try
            {
                var stream = await _minioFileService.GetFileAsync(key);
                var contentType = GetContentType(key);
                Response.Headers["Cache-Control"] = "public, max-age=86400";
                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = "Dosya bulunamadı.", error = ex.Message });
            }
        }

        private static string GetContentType(string key)
        {
            var ext = Path.GetExtension(key).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".webm" => "audio/webm",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".mp4" => "video/mp4",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        [HttpGet("url/{key}")]
        public IActionResult GetUrl(string key)
        {
            var url = _minioFileService.GetFileUrl(key);
            return Ok(new { Url = url });
        }
    }
}
