using Microsoft.AspNetCore.Mvc;
using ProductManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;


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
    public async Task<IActionResult> Register([FromBody] Users model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        _userAuthService.Register(model);
        return Ok();
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        try
        {
            var token = await _userAuthService.Login(model);
            return Ok(new { token });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch
        {
            // Genel hata yönetimi
            return StatusCode(500, "Internal server error.");
        }
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        // Mevcut kullanıcının ID'sini al
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var users = _userAuthService.GetUsers(currentUserId);
        return Ok(users);
    }


    [Authorize]
    public async Task<IActionResult> GetUsersAsync()
    {
        // Mevcut kullanıcının ID'sini al
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var users = _userAuthService.GetUsersAsync();
        return Ok(users);
    }

}

