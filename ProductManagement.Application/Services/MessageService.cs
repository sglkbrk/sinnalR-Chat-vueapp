using ProductManagement.Infrastructure.Data;

using ProductManagement.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ProductManagement.Application.Services
{
    public class MessageService
    {
        private readonly ApplicationDbContext _context;
        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Message> GetAll()
        {
            return _context.Message.ToList();
        }

        public List<Message> GetMyMessages(int senderId, int receiverId, int page = 1, int pageSize = 50)
        {
            var messages = _context.Message.
            Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == senderId))
            .Include(m => m.Sender)
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
            return messages.Reverse().ToList();
        }

        public Message? GetMessageById(int id)
        {
            return _context.Message.FirstOrDefault(c => c.Id == id);
        }

        public async Task AddMessageAsync(Message message)
        {
            _context.Message.Add(message);
            await _context.SaveChangesAsync();
        }


        public void UpdateMessage(Message message)
        {
            _context.Message.Update(message);
            _context.SaveChanges();
        }

        public void DeleteMessage(int id)
        {
            var message = GetMessageById(id);
            if (message != null)
            {
                _context.Message.Remove(message);
                _context.SaveChanges();
            }
        }

        public async Task setSeen(int senderId, int receiverId, messageStatus messageStatus)
        {
            IQueryable<Message> query = _context.Message
        .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId);

            if (messageStatus == messageStatus.Seen)
            {
                query = query.Where(m => m.Status != messageStatus.Seen);
            }

            else if (messageStatus == messageStatus.forwarded)
            {
                query = query.Where(m => m.Status == messageStatus.Sent);
            }
            var messages = await query.ToListAsync();
            foreach (var message in messages)
            {
                message.Status = messageStatus;
            }

            // Veritabanına değişiklikleri kaydediyoruz
            await _context.SaveChangesAsync();

        }
    }
}