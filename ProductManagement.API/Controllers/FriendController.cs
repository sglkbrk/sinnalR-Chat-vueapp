using Microsoft.AspNetCore.Mvc;
using ProductManagement.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;


namespace ProductManagement.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendController : Controller
    {
        private readonly FriendService _friendService;
        public FriendController(FriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpGet("GetChatFriends")]
        public IActionResult GetChatFriends()
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Ok(_friendService.GetChatFriends(int.Parse(currentUserId ?? "0")));
        }








    }
}