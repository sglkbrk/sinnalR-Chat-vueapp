using Microsoft.AspNetCore.Mvc;
using ProductManagement.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
using Microsoft.AspNetCore.SignalR;


namespace ProductManagement.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendRequestController : Controller
    {
        private readonly FriendRequestService _friendRequestService;
        private readonly IHubContext<ChatHub> _chatHub;
        public FriendRequestController(FriendRequestService friendRequestService, IHubContext<ChatHub> chatHub)
        {
            _friendRequestService = friendRequestService;
            _chatHub = chatHub;
        }

        [HttpGet("{id}")]
        public IActionResult GetFriendRequestById(int id)
        {
            return Ok(_friendRequestService.GetFriendRequestById(id));
        }

        [HttpGet("GetFriendRequestByReceiverId/{id}")]
        public IActionResult GetFriendRequestByReceiverId(int id)
        {
            return Ok(_friendRequestService.GetFriendRequestByReceiverId(id));
        }

        [HttpGet("GetFriendRequestBySenderId/{id}")]
        public IActionResult GetFriendRequestBySenderId(int id)
        {
            return Ok(_friendRequestService.GetFriendRequestBySenderId(id));
        }
        [HttpPost]
        public IActionResult AddFriendRequest([FromBody] FriendRequest friendRequest)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null) return BadRequest();
            friendRequest.SenderId = int.Parse(currentUserId ?? "0");
            _friendRequestService.AddFriendRequest(friendRequest);
            _chatHub.Clients.User(friendRequest.ReceiverId.ToString()).SendAsync("ReceiveNotification", new Dictionary<string, string> {
                { "type", "refreshFriendRequests" },
                // { "name", friendRequest.Sender.Username },
                // { "id", friendRequest.Sender.id.ToString() }

            });
            return Ok(friendRequest);
        }

        [HttpPut]
        public IActionResult UpdateFriendRequest([FromBody] FriendRequest friendRequest)
        {
            _friendRequestService.UpdateFriendRequest(friendRequest);
            return Ok(friendRequest);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteFriendRequest(int id)
        {
            _friendRequestService.DeleteFriendRequest(id);
            return Ok();
        }

        [HttpPut("AcceptAndRejectFriendRequest/{id}/{friendRequestStatus}")]
        public IActionResult AcceptAndRejectFriendRequest(int id, FriendRequestStatus friendRequestStatus)
        {
            var friendRequest = _friendRequestService.GetFriendRequestById(id);
            _friendRequestService.AcceptAndRejectFriendRequest(id, friendRequestStatus);
            if (friendRequestStatus.Equals(FriendRequestStatus.Accepted)) _chatHub.Clients.User(friendRequest.SenderId.ToString()).SendAsync("ReceiveNotification", new Dictionary<string, string> {
                { "type", "refreshUsers" },
                { "name", friendRequest.Receiver.Username },
                { "id", friendRequest.Receiver.id.ToString() }

            });
            return Ok();
        }
    }
}