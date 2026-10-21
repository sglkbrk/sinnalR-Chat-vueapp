using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using FirebaseAdmin;
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
            return _context.FbToken.OrderByDescending(c => c.LastUpdatedAt).FirstOrDefault(c => c.UserId == userId);
        }

        public List<string> GetTokensByUserId(int userId)
        {
            return _context.FbToken
                .Where(c => c.UserId == userId)
                .Select(c => c.Token)
                .ToList();
        }

        public void AddToken(FbToken token)
        {
            var existing = _context.FbToken.FirstOrDefault(c => c.Token == token.Token);
            if (existing == null)
            {
                token.LastUpdatedAt = DateTime.UtcNow;
                _context.FbToken.Add(token);
            }
            else
            {
                existing.UserId = token.UserId;
                existing.DeviceInfo = token.DeviceInfo ?? existing.DeviceInfo;
                existing.LastUpdatedAt = DateTime.UtcNow;
                _context.FbToken.Update(existing);
            }
            _context.SaveChanges();
        }

        public async Task SendNotificationToUserAsync(int userId, string body, string name, string senderId)
        {
            if (FirebaseApp.DefaultInstance == null) return;
            var tokens = GetTokensByUserId(userId);
            foreach (var token in tokens)
            {
                await SendNotificationAsync(body, name, token, senderId);
            }
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
            if (FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("[Push] FirebaseApp başlatılmadığı için bildirim gönderilemedi.");
                return;
            }

            var message = new FirebaseAdmin.Messaging.Message()
            {
                Token = token,
                Data = new Dictionary<string, string>()
                {
                    { "url", $"/?chat={senderId}" },
                    { "senderId", senderId },
                    { "name", name ?? "Yeni Mesaj" },
                    { "body", body ?? "" },
                    { "image", $"https://randomuser.me/api/portraits/men/{senderId}.jpg" }
                },
                Webpush = new WebpushConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Urgency", "high" },
                        { "TTL", "86400" }
                    },
                    Notification = new WebpushNotification
                    {
                        Title = name ?? "Yeni Mesaj",
                        Body = body ?? "",
                        Icon = $"https://randomuser.me/api/portraits/men/{senderId}.jpg",
                        Tag = $"chat-{senderId}"
                    }
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine("[Push] Başarıyla gönderildi: " + response);
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine($"[Push Hata] FirebaseMessagingException: {ex.Message} (Kod: {ex.MessagingErrorCode})");
                if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                {
                    Console.WriteLine($"[Push Temizlik] Geçersiz token veritabanından kaldırılıyor: {token}");
                    DeleteToken(token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Push Hata] Genel hata: {ex.Message}");
            }
        }
    }
}