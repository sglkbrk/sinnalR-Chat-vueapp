using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
using ProductManagement.Application.DTO;
using ProductManagement.Application.Common;
using System.Security.Claims;

namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly FriendRequestService _friendRequestService;

        public UsersController(UserService userService, FriendRequestService friendRequestService)
        {
            _userService = userService;
            _friendRequestService = friendRequestService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id))
            {
                return id;
            }
            throw new AppException("Yetkilendirme doğrulanamadı.", System.Net.HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            int currentUserId = GetCurrentUserId();
            var profile = await _userService.GetProfileAsync(currentUserId);
            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            int currentUserId = GetCurrentUserId();
            await _userService.UpdateProfileAsync(currentUserId, request);
            return Ok(ApiResponse.Ok());
        }

        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            int currentUserId = GetCurrentUserId();
            var avatarKey = await _userService.UpdateAvatarAsync(currentUserId, file);
            return Ok(ApiResponse<object>.Ok(new { avatarUrl = avatarKey }));
        }

        [HttpDelete("me/avatar")]
        public async Task<IActionResult> RemoveAvatar()
        {
            int currentUserId = GetCurrentUserId();
            await _userService.RemoveAvatarAsync(currentUserId);
            return Ok(ApiResponse.Ok());
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string q)
        {
            int currentUserId = GetCurrentUserId();
            var results = _friendRequestService.SearchUsers(q, currentUserId);
            return Ok(ApiResponse<List<UserSearchResultDto>>.Ok(results));
        }
    }
}

