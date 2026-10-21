using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
using ProductManagement.Application.Common;
using ProductManagement.Domain.Models;

[Route("api/[controller]")]
[ApiController]
public class UserAuthController : ControllerBase
{
    private readonly UserAuthService _userAuthService;

    public UserAuthController(UserAuthService userAuthService)
    {
        _userAuthService = userAuthService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] Users model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Geçersiz form verisi.";
            return BadRequest(new { success = false, message = firstError, error = new { code = "VALIDATION_ERROR", message = firstError } });
        }

        try
        {
            _userAuthService.Register(model);
            return Ok(new { success = true, message = "Kayıt başarılı." });
        }
        catch (AppException ex)
        {
            return StatusCode((int)ex.StatusCode, new { success = false, message = ex.Message, error = new { code = ex.ErrorCode, message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message, error = new { code = "SERVER_ERROR", message = ex.Message } });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Kullanıcı adı ve şifre gereklidir.", error = new { code = "VALIDATION_ERROR", message = "Kullanıcı adı ve şifre gereklidir." } });
        }

        try
        {
            var (token, user) = await _userAuthService.Login(model);
            return Ok(new
            {
                success = true,
                token = token,
                data = new
                {
                    token = token,
                    user = new
                    {
                        id = user.id,
                        username = user.Username,
                        email = user.Email
                    }
                }
            });
        }
        catch (AppException ex)
        {
            return StatusCode((int)ex.StatusCode, new { success = false, message = ex.Message, error = new { code = ex.ErrorCode, message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Giriş sırasında bir hata oluştu.", error = new { code = "SERVER_ERROR", message = ex.Message } });
        }
    }

    [Authorize]
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var users = _userAuthService.GetUsers(currentUserId ?? "");
        return Ok(users);
    }

    [Authorize]
    [HttpGet("users-async")]
    public IActionResult GetUsersAsync()
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var users = _userAuthService.GetUsersAsync(currentUserId ?? "");
        return Ok(users);
    }
}

