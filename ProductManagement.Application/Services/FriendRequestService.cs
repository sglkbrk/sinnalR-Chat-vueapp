using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using Microsoft.EntityFrameworkCore;

// using Microsoft.EntityFrameworkCore;

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
            return _context.FriendRequest.Include(c => c.Sender).Include(c => c.Receiver).FirstOrDefault(c => c.Id == id);
        }

        public List<FriendRequest> GetFriendRequestByReceiverId(int id)
        {
            return _context.FriendRequest.Where(c => c.ReceiverId == id && c.Status == 0).Include(c => c.Sender).Include(c => c.Receiver).ToList();
        }

        public List<FriendRequest> GetFriendRequestBySenderId(int id)
        {
            return _context.FriendRequest.Where(c => c.SenderId == id && c.Status == 0).Include(c => c.Sender).Include(c => c.Receiver).ToList();
        }

        public void AddFriendRequest(FriendRequest friendRequest)
        {
            var user = _context.Users.Where(u => u.id == friendRequest.ReceiverId).FirstOrDefault();
            if (user == null)
            {
                throw new Exception("Bu kullanıcı bulunamadı.");
            }
            var existingFriendRequest = _context.FriendRequest
            .Where(c => ((c.ReceiverId == friendRequest.ReceiverId && c.SenderId == friendRequest.SenderId) || (c.ReceiverId == friendRequest.SenderId && c.SenderId == friendRequest.ReceiverId)) && !c.Status.Equals(FriendRequestStatus.Declined))
            .ToList();
            if (existingFriendRequest.Count == 0)
            {
                _context.FriendRequest.Add(friendRequest);
                _context.SaveChanges();

            }
            else
            {
                throw new Exception("Bu kullanıcıya zaten bir arkadaşlık isteği gönderdiniz veya arkadaşsınız.");
            }

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

        public void AcceptAndRejectFriendRequest(int id, FriendRequestStatus friendRequestStatus)
        {
            var friendRequest = GetFriendRequestById(id);
            if (friendRequest != null)
            {
                friendRequest.Status = friendRequestStatus;
                friendRequest.RespondedAt = DateTime.Now;
                _context.FriendRequest.Update(friendRequest);
                if (friendRequestStatus.Equals(FriendRequestStatus.Accepted))
                {
                    Friend friend = new Friend();
                    friend.UserId = friendRequest.SenderId;
                    friend.FriendUserId = friendRequest.ReceiverId;
                    friend.FriendsSince = DateTime.Now;
                    _friendService.AddFriend(friend);
                    Friend friend2 = new Friend();
                    friend2.UserId = friendRequest.ReceiverId;
                    friend2.FriendUserId = friendRequest.SenderId;
                    friend2.FriendsSince = DateTime.Now;
                    _friendService.AddFriend(friend2);
                }
                _context.SaveChanges();
            }

        }
    }
}