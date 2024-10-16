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

        [HttpGet("download/{key}")]
        public async Task<IActionResult> Download(string key)
        {
            var stream = await _minioFileService.GetFileAsync(key);
            // İçerik tipini belirlemek için dosya uzantısına göre ayarlayabilirsiniz
            return File(stream, "application/octet-stream", key);
        }

        [HttpGet("url/{key}")]
        public IActionResult GetUrl(string key)
        {
            var url = _minioFileService.GetFileUrl(key);
            return Ok(new { Url = url });
        }
    }
}
