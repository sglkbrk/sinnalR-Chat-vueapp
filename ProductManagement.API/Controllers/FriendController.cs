using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
using ProductManagement.Application.Common;
using System.Security.Claims;

namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly FriendService _friendService;

        public FriendController(FriendService friendService)
        {
            _friendService = friendService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id)) return id;
            throw new AppException("Yetkisiz işlem.", System.Net.HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        }

        [HttpGet("GetChatFriends")]
        public IActionResult GetChatFriends()
        {
            int currentUserId = GetCurrentUserId();
            var friends = _friendService.GetChatFriends(currentUserId);
            return Ok(friends);
        }

        public class UpdateFriendSettingsInput
        {
            public bool? IsPinned { get; set; }
            public bool? IsMuted { get; set; }
            public bool? IsArchived { get; set; }
        }

        [HttpPatch("settings/{friendUserId}")]
        public IActionResult UpdateFriendSettings(int friendUserId, [FromBody] UpdateFriendSettingsInput input)
        {
            int currentUserId = GetCurrentUserId();
            _friendService.UpdateFriendSettings(currentUserId, friendUserId, input.IsPinned, input.IsMuted, input.IsArchived);
            return Ok(ApiResponse.Ok());
        }

        [HttpDelete("remove/{friendUserId}")]
        public IActionResult RemoveFriend(int friendUserId)
        {
            int currentUserId = GetCurrentUserId();
            _friendService.RemoveFriend(currentUserId, friendUserId);
            return Ok(ApiResponse.Ok());
        }
    }
}
