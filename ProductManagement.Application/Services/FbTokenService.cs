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
            FbToken userToken = GetTokenByUserId(token.UserId);
            if (userToken == null)
            {
                _context.FbToken.Add(token);
            }
            else
            {
                userToken.Token = token.Token;
                _context.FbToken.Update(userToken);

            }
            _context.SaveChanges();
            // FbToken tokess = GetTokenByUserId(token.UserId);
            // if (tokess == null)
            // {
            //     FbToken token1 = _context.FbToken.FirstOrDefault(c => c.Token == token.Token);
            //     if (token1 == null)
            //     {
            //         _context.FbToken.Add(token);
            //         _context.SaveChanges();
            //     }
            //     else
            //     {
            //         UpdateToken(tokess);
            //     }

            // }
            // else
            // {
            //     UpdateToken(tokess);
            // }

        }

        public void UpdateToken(FbToken token)
        {
            _context.FbToken.Update(token);
            _context.SaveChanges();
        }

        public void DeleteToken(int userId)
        {
            var fbToken = GetTokenByUserId(userId);
            if (fbToken != null)
            {
                _context.FbToken.Remove(fbToken);
                _context.SaveChanges();
            }
        }
        public void DeleteToken(string token)
        {
            var fbToken = _context.FbToken.FirstOrDefault(c => c.Token == token);
            if (fbToken != null)
            {
                _context.FbToken.Remove(fbToken);
                _context.SaveChanges();
            }
        }

        public async Task SendNotificationAsync(string body, string name, string token, string senderId)
        {
            var message = new FirebaseAdmin.Messaging.Message()
            {
                Token = token,
                Data = new Dictionary<string, string>()
                {
                    { "url", "https://chat.buraksaglik.com/" },
                    { "senderId", senderId },
                    { "name", name },
                    { "body", body },
                    { "image", "https://randomuser.me/api/portraits/men/" + senderId + ".jpg" }
                },
            };
            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine("Successfully sent message: " + response);
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine($"FirebaseMessagingException: {ex.Message}");
            }


            // string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            // Console.WriteLine("Bildirim gönderildi: " + response);
        }
    }
}