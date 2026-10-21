using Microsoft.AspNetCore.Mvc;
using ProductManagement.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
using ProductManagement.Application.Common;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendRequestController : ControllerBase
    {
        private readonly FriendRequestService _friendRequestService;
        private readonly IHubContext<ChatHub> _chatHub;

        public FriendRequestController(FriendRequestService friendRequestService, IHubContext<ChatHub> chatHub)
        {
            _friendRequestService = friendRequestService;
            _chatHub = chatHub;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id)) return id;
            throw new AppException("Yetkisiz işlem.", System.Net.HttpStatusCode.Unauthorized, "UNAUTHORIZED");
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

        public class SendFriendRequestInput
        {
            public int? ReceiverId { get; set; }
            public string? Username { get; set; }
        }

        [HttpPost]
        public IActionResult AddFriendRequest([FromBody] SendFriendRequestInput input)
        {
            int currentUserId = GetCurrentUserId();
            var request = _friendRequestService.SendFriendRequest(currentUserId, input.Username, input.ReceiverId);

            _chatHub.Clients.User(request.ReceiverId.ToString()).SendAsync("ReceiveNotification", new Dictionary<string, string> {
                { "type", "refreshFriendRequests" }
            });

            return Ok(request);
        }

        [HttpPost("cancel/{id}")]
        public IActionResult CancelFriendRequest(int id)
        {
            int currentUserId = GetCurrentUserId();
            _friendRequestService.CancelFriendRequest(id, currentUserId);
            return Ok(ApiResponse.Ok());
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
            return Ok(ApiResponse.Ok());
        }

        [HttpPut("AcceptAndRejectFriendRequest/{id}/{friendRequestStatus}")]
        public IActionResult AcceptAndRejectFriendRequest(int id, FriendRequestStatus friendRequestStatus)
        {
            int currentUserId = GetCurrentUserId();
            var friendRequest = _friendRequestService.GetFriendRequestById(id);
            if (friendRequest == null) return NotFound();

            _friendRequestService.AcceptAndRejectFriendRequest(id, friendRequestStatus, currentUserId);

            if (friendRequestStatus == FriendRequestStatus.Accepted)
            {
                _chatHub.Clients.User(friendRequest.SenderId.ToString()).SendAsync("ReceiveNotification", new Dictionary<string, string> {
                    { "type", "refreshUsers" },
                    { "name", friendRequest.Receiver?.Username ?? "Arkadaş" },
                    { "id", friendRequest.ReceiverId.ToString() }
                });
            }

            return Ok(ApiResponse.Ok());
        }
    }
}
