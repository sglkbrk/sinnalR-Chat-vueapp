using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using FirebaseAdmin.Messaging;

namespace ProductManagement.Application.Services
{
    public class FbTokenService
    {
        private readonly ApplicationDbContext _context;
        public FbTokenService(ApplicationDbContext context)
        {
            _context = context;


        }

        public List<FbToken> GetAllToken()
        {
            return _context.FbToken.ToList();
        }

        public FbToken? GetTokenByUserId(int userId)
        {
            return _context.FbToken.FirstOrDefault(c => c.UserId == userId);
        }

        public void AddToken(FbToken token)
        {
            FbToken tokess = GetTokenByUserId(token.UserId);
            if (tokess == null)
            {
                FbToken token1 = _context.FbToken.FirstOrDefault(c => c.Token == token.Token);
                if (token1 == null)
                {
                    _context.FbToken.Add(token);
                    _context.SaveChanges();
                }
                else
                {
                    UpdateToken(tokess);
                }

            }
            else
            {
                UpdateToken(tokess);
            }

        }

        public void UpdateToken(FbToken token)
        {
            _context.FbToken.Update(token);
            _context.SaveChanges();
        }

        public void DeleteCategory(int userId)
        {
            var fbToken = GetTokenByUserId(userId);
            if (fbToken != null)
            {
                _context.FbToken.Remove(fbToken);
                _context.SaveChanges();
            }
        }

        public async Task SendNotificationAsync(string title, string body, string token, string senderId)
        {
            var message = new FirebaseAdmin.Messaging.Message()
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                    ImageUrl = "https://randomuser.me/api/portraits/men/" + senderId + ".jpg"

                },
                Data = new Dictionary<string, string>()
                {
                    { "url", "http://localhost:5044/" },
                    { "senderId", senderId },
                },
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Console.WriteLine("Bildirim gönderildi: " + response);
        }
    }
}