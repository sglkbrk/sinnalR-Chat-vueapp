using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using ProductManagement.Application.DTO;
using ProductManagement.Application.Common;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ProductManagement.Application.Services
{
    public class FriendRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly FriendService _friendService;

        public FriendRequestService(ApplicationDbContext context, FriendService friendService)
        {
            _context = context;
            _friendService = friendService;
        }

        public FriendRequest? GetFriendRequestById(int id)
        {
            return _context.FriendRequest
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .FirstOrDefault(c => c.Id == id);
        }

        public List<FriendRequest> GetFriendRequestByReceiverId(int id)
        {
            return _context.FriendRequest
                .Where(c => c.ReceiverId == id && c.Status == FriendRequestStatus.Pending)
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .OrderByDescending(c => c.RequestedAt)
                .ToList();
        }

        public List<FriendRequest> GetFriendRequestBySenderId(int id)
        {
            return _context.FriendRequest
                .Where(c => c.SenderId == id && c.Status == FriendRequestStatus.Pending)
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .OrderByDescending(c => c.RequestedAt)
                .ToList();
        }

        public FriendRequest SendFriendRequest(int senderId, string? targetUsername, int? targetUserId)
        {
            if (targetUserId.HasValue && targetUserId.Value == senderId)
            {
                throw new AppException("Kendinize arkadaşlık isteği gönderemezsiniz.", HttpStatusCode.BadRequest, "CANNOT_FRIEND_SELF");
            }

            Users? targetUser = null;

            if (targetUserId.HasValue && targetUserId.Value > 0)
            {
                targetUser = _context.Users.FirstOrDefault(u => u.id == targetUserId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(targetUsername))
            {
                var trimmed = targetUsername.Trim();
                targetUser = _context.Users.FirstOrDefault(u => u.Username.ToLower() == trimmed.ToLower() || u.Email.ToLower() == trimmed.ToLower());
            }

            if (targetUser == null)
            {
                throw new AppException("Kullanıcı bulunamadı.", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            }

            if (targetUser.id == senderId)
            {
                throw new AppException("Kendinize arkadaşlık isteği gönderemezsiniz.", HttpStatusCode.BadRequest, "CANNOT_FRIEND_SELF");
            }

            if (_friendService.IsFriend(senderId, targetUser.id))
            {
                throw new AppException("Bu kullanıcıyla zaten arkadaşsınız.", HttpStatusCode.Conflict, "ALREADY_FRIENDS");
            }

            // Bekleyen istek kontrolü
            var existingRequest = _context.FriendRequest
                .FirstOrDefault(c => (c.ReceiverId == targetUser.id && c.SenderId == senderId) ||
                                     (c.ReceiverId == senderId && c.SenderId == targetUser.id));

            if (existingRequest != null)
            {
                if (existingRequest.Status == FriendRequestStatus.Pending)
                {
                    if (existingRequest.SenderId == senderId)
                    {
                        throw new AppException("Bu kullanıcıya zaten bekleyen bir arkadaşlık isteğiniz var.", HttpStatusCode.Conflict, "REQUEST_ALREADY_PENDING");
                    }
                    else
                    {
                        // Karşı taraf zaten istek atmışsa direkt kabul et
                        AcceptAndRejectFriendRequest(existingRequest.Id, FriendRequestStatus.Accepted, senderId);
                        return existingRequest;
                    }
                }

                // Eski reddedilmiş veya iptal edilmiş istek varsa yeniden Pending yap
                existingRequest.SenderId = senderId;
                existingRequest.ReceiverId = targetUser.id;
                existingRequest.Status = FriendRequestStatus.Pending;
                existingRequest.RequestedAt = DateTime.UtcNow;
                existingRequest.RespondedAt = null;
                _context.FriendRequest.Update(existingRequest);
                _context.SaveChanges();
                return existingRequest;
            }

            var newRequest = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = targetUser.id,
                Status = FriendRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.FriendRequest.Add(newRequest);
            _context.SaveChanges();
            return newRequest;
        }

        public void AddFriendRequest(FriendRequest friendRequest)
        {
            SendFriendRequest(friendRequest.SenderId, null, friendRequest.ReceiverId);
        }

        public void CancelFriendRequest(int id, int currentUserId)
        {
            var request = GetFriendRequestById(id);
            if (request == null)
            {
                throw new AppException("İstek bulunamadı.", HttpStatusCode.NotFound, "NOT_FOUND");
            }

            if (request.SenderId != currentUserId)
            {
                throw new AppException("Bu isteği iptal etme yetkiniz yok.", HttpStatusCode.Forbidden, "FORBIDDEN");
            }

            request.Status = FriendRequestStatus.Canceled;
            request.RespondedAt = DateTime.UtcNow;
            _context.FriendRequest.Update(request);
            _context.SaveChanges();
        }

        public void AcceptAndRejectFriendRequest(int id, FriendRequestStatus friendRequestStatus, int currentUserId)
        {
            var friendRequest = GetFriendRequestById(id);
            if (friendRequest == null)
            {
                throw new AppException("Arkadaşlık isteği bulunamadı.", HttpStatusCode.NotFound, "NOT_FOUND");
            }

            if (friendRequest.ReceiverId != currentUserId)
            {
                throw new AppException("Bu isteği yanıtlama yetkiniz yok.", HttpStatusCode.Forbidden, "FORBIDDEN");
            }

            friendRequest.Status = friendRequestStatus;
            friendRequest.RespondedAt = DateTime.UtcNow;
            _context.FriendRequest.Update(friendRequest);

            if (friendRequestStatus == FriendRequestStatus.Accepted)
            {
                if (!_friendService.IsFriend(friendRequest.SenderId, friendRequest.ReceiverId))
                {
                    _friendService.AddFriend(new Friend
                    {
                        UserId = friendRequest.SenderId,
                        FriendUserId = friendRequest.ReceiverId,
                        FriendsSince = DateTime.UtcNow
                    });
                }

                if (!_friendService.IsFriend(friendRequest.ReceiverId, friendRequest.SenderId))
                {
                    _friendService.AddFriend(new Friend
                    {
                        UserId = friendRequest.ReceiverId,
                        FriendUserId = friendRequest.SenderId,
                        FriendsSince = DateTime.UtcNow
                    });
                }
            }

            _context.SaveChanges();
        }

        public void AcceptAndRejectFriendRequest(int id, FriendRequestStatus friendRequestStatus)
        {
            var friendRequest = GetFriendRequestById(id);
            if (friendRequest != null)
            {
                AcceptAndRejectFriendRequest(id, friendRequestStatus, friendRequest.ReceiverId);
            }
        }

        public List<UserSearchResultDto> SearchUsers(string query, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<UserSearchResultDto>();
            }

            var trimmed = query.Trim().ToLower();

            var searchCode = trimmed.StartsWith("#") ? trimmed.Substring(1) : trimmed;

            var matchingUsers = _context.Users.AsNoTracking()
                .Where(u => u.id != currentUserId &&
                            (u.Username.ToLower().Contains(trimmed) ||
                             u.Email.ToLower().Contains(trimmed) ||
                             u.id.ToString() == trimmed ||
                             (u.UserCode != null && (u.UserCode.ToLower().Contains(trimmed) || u.UserCode.ToLower().Contains(searchCode)))))
                .Take(20)
                .ToList();

            var userIds = matchingUsers.Select(u => u.id).ToList();

            var existingFriendIds = _context.Friend.AsNoTracking()
                .Where(f => f.UserId == currentUserId && userIds.Contains(f.FriendUserId))
                .Select(f => f.FriendUserId)
                .ToHashSet();

            var pendingRequests = _context.FriendRequest.AsNoTracking()
                .Where(r => r.Status == FriendRequestStatus.Pending &&
                            ((r.SenderId == currentUserId && userIds.Contains(r.ReceiverId)) ||
                             (r.ReceiverId == currentUserId && userIds.Contains(r.SenderId))))
                .ToList();

            var results = new List<UserSearchResultDto>();
            foreach (var user in matchingUsers)
            {
                var incoming = pendingRequests.FirstOrDefault(r => r.SenderId == user.id && r.ReceiverId == currentUserId);
                var outgoing = pendingRequests.FirstOrDefault(r => r.SenderId == currentUserId && r.ReceiverId == user.id);

                results.Add(new UserSearchResultDto
                {
                    Id = user.id,
                    Username = user.Username,
                    AvatarUrl = user.AvatarUrl,
                    StatusMessage = user.StatusMessage,
                    IsFriend = existingFriendIds.Contains(user.id),
                    HasPendingIncoming = incoming != null,
                    HasPendingOutgoing = outgoing != null,
                    PendingRequestId = incoming?.Id ?? outgoing?.Id
                });
            }

            return results;
        }

        public void UpdateFriendRequest(FriendRequest friendRequest)
        {
            _context.FriendRequest.Update(friendRequest);
            _context.SaveChanges();
        }

        public void DeleteFriendRequest(int id)
        {
            var friendRequest = GetFriendRequestById(id);
            if (friendRequest != null)
            {
                _context.FriendRequest.Remove(friendRequest);
                _context.SaveChanges();
            }
        }
    }
}
