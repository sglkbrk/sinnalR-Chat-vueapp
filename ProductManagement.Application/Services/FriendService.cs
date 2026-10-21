using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using ProductManagement.Application.DTO;
using Microsoft.EntityFrameworkCore;

namespace ProductManagement.Application.Services
{
    public class FriendService
    {
        private readonly ApplicationDbContext _context;

        public FriendService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Friend? GetFriendById(int id)
        {
            return _context.Friend.FirstOrDefault(c => c.Id == id);
        }

        public List<Friend> GetFriends(int userId)
        {
            return _context.Friend.Where(c => c.UserId == userId).ToList();
        }

        public bool IsFriend(int userId, int otherUserId)
        {
            return _context.Friend.Any(c => c.UserId == userId && c.FriendUserId == otherUserId);
        }

        public List<Friends> GetChatFriends(int userId)
        {
            var rawFriends = _context.Friend.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    FriendshipId = u.Id,
                    Friend = u.FriendUser,
                    IsPinned = u.IsPinned,
                    IsMuted = u.IsMuted,
                    IsArchived = u.IsArchived,
                    LastMessage = _context.Message
                        .Where(m => (m.SenderId == u.FriendUserId && m.ReceiverId == userId) ||
                                    (m.ReceiverId == u.FriendUserId && m.SenderId == userId))
                        .OrderByDescending(m => m.Timestamp)
                        .FirstOrDefault(),
                    NotSeenMessagesCount = _context.Message
                        .Count(m => m.ReceiverId == userId &&
                                    m.SenderId == u.FriendUserId &&
                                    m.Status != messageStatus.Seen)
                })
                .AsEnumerable()
                .Select(u => new Friends
                {
                    Id = u.Friend.id,
                    UserName = u.Friend.Username,
                    Email = u.Friend.Email,
                    AvatarUrl = u.Friend.AvatarUrl,
                    StatusMessage = u.Friend.StatusMessage,
                    LastSeenAt = u.Friend.LastSeenAt,
                    IsPinned = u.IsPinned,
                    IsMuted = u.IsMuted,
                    IsArchived = u.IsArchived,
                    LastMessage = u.LastMessage != null ? new MessageDto
                    {
                        Content = u.LastMessage.Content,
                        Timestamp = u.LastMessage.Timestamp
                    } : null,
                    NotSeenMessagesCount = u.NotSeenMessagesCount
                })
                .OrderByDescending(f => f.IsPinned)
                .ThenByDescending(f => f.LastMessage != null ? f.LastMessage.Timestamp : DateTime.MinValue)
                .ToList();

            return rawFriends;
        }

        public void UpdateFriendSettings(int userId, int friendUserId, bool? isPinned = null, bool? isMuted = null, bool? isArchived = null)
        {
            var friendship = _context.Friend.FirstOrDefault(f => f.UserId == userId && f.FriendUserId == friendUserId);
            if (friendship != null)
            {
                if (isPinned.HasValue) friendship.IsPinned = isPinned.Value;
                if (isMuted.HasValue) friendship.IsMuted = isMuted.Value;
                if (isArchived.HasValue) friendship.IsArchived = isArchived.Value;
                _context.SaveChanges();
            }
        }

        public void AddFriend(Friend friend)
        {
            _context.Friend.Add(friend);
            _context.SaveChanges();
        }

        public void UpdateFriend(Friend friend)
        {
            _context.Friend.Update(friend);
            _context.SaveChanges();
        }

        public void DeleteFriend(int id)
        {
            var friend = GetFriendById(id);
            if (friend != null)
            {
                _context.Friend.Remove(friend);
                _context.SaveChanges();
            }
        }
    }
}
