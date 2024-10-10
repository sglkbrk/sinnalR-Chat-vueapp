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
            return _context.Message.
            Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == senderId))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderBy(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        }

        public Message? GetMessageById(int id)
        {
            return _context.Message.FirstOrDefault(c => c.Id == id);
        }

        public void AddMessage(Message message)
        {
            _context.Message.Add(message);
            _context.SaveChanges();
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
    }
}