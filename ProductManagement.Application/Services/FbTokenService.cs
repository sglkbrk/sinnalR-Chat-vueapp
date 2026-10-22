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

        private string GetSenderAvatarUrl(string senderId, string senderName)
        {
            if (int.TryParse(senderId, out int sId) && sId > 0)
            {
                var user = _context.Users.FirstOrDefault(u => u.id == sId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(user.AvatarUrl) && !user.AvatarUrl.Contains("randomuser.me"))
                    {
                        if (user.AvatarUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            user.AvatarUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            return user.AvatarUrl;
                        }

                        return $"/api/MinioFile/download/{user.AvatarUrl}";
                    }

                    if (!string.IsNullOrWhiteSpace(user.Username))
                    {
                        senderName = user.Username;
                    }
                }
            }

            var displayName = string.IsNullOrWhiteSpace(senderName) ? "User" : senderName;
            return $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(displayName)}&background=4f46e5&color=fff&rounded=true&size=192";
        }

        public async Task SendNotificationToUserAsync(int userId, string body, string name, string senderId)
        {
            if (FirebaseApp.DefaultInstance == null) return;
            var tokens = GetTokensByUserId(userId);
            if (tokens == null || !tokens.Any()) return;

            string avatarUrl = GetSenderAvatarUrl(senderId, name);

            foreach (var token in tokens)
            {
                await SendNotificationAsync(body, name, token, senderId, avatarUrl);
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

        public async Task SendNotificationAsync(string body, string name, string token, string senderId, string? avatarUrl = null)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("[Push] FirebaseApp başlatılmadığı için bildirim gönderilemedi.");
                return;
            }

            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                avatarUrl = GetSenderAvatarUrl(senderId, name);
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
                    { "image", avatarUrl }
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
                        Icon = avatarUrl,
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