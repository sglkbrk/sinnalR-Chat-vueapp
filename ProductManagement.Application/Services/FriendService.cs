using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
// using Microsoft.EntityFrameworkCore;

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

        public List<Friends> GetChatFriends(int userId)
        {
            var users = _context.Friend
           .Where(u => u.UserId == userId)
           .Select(u => new Friends
           {
               Id = u.FriendUser.id,
               UserName = u.FriendUser.Username,
               Email = u.FriendUser.Email,
               LastMessage = _context.Message
                   .Where(m => (m.SenderId == u.FriendUser.id && m.ReceiverId == userId) ||
                               (m.ReceiverId == u.FriendUser.id && m.SenderId == userId))
                   .OrderByDescending(m => m.Timestamp)
                   .FirstOrDefault(),
               NotSeenMessagesCount = _context.Message.Where(m => m.ReceiverId == userId && m.SenderId == u.FriendUser.id && m.Status != messageStatus.Seen).Count(),
           }).ToList();

            return users;
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