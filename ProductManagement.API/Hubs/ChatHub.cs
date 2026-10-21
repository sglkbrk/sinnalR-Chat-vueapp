using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ProductManagement.Domain.Models;
using ProductManagement.Application.Services;
using System.Collections.Concurrent;

[Authorize]
public class ChatHub : Hub
{
    // Kullanıcıların online durumlarını tutmak için ConcurrentDictionary
    private static ConcurrentDictionary<string, string> _onlineUsers = new ConcurrentDictionary<string, string>();
    // Phase 5: Dürtme spam önleme (10 saniye bekleme süresi)
    private static ConcurrentDictionary<int, DateTime> _buzzCooldowns = new ConcurrentDictionary<int, DateTime>();

    private readonly MessageService _messageService;
    private readonly FbTokenService _fbTokenService;

    public ChatHub(MessageService messageService, FbTokenService fbTokenService)
    {
        _messageService = messageService;
        _fbTokenService = fbTokenService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers.TryAdd(Context.ConnectionId, userId);
            await Clients.All.SendAsync("UserStatusUpdated", _onlineUsers.Values.ToList());
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _onlineUsers.TryRemove(Context.ConnectionId, out _);
        await Clients.All.SendAsync("UserStatusUpdated", _onlineUsers.Values.ToList());
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Message> SendMessage(Message message)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.UserIdentifier;
        var name = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Bilinmeyen";
        if (string.IsNullOrEmpty(senderId))
        {
            throw new HubException("Kullanıcı kimliği doğrulanamadı.");
        }

        message.SenderId = int.Parse(senderId);
        message.Timestamp = DateTime.UtcNow;

        // Phase 5: Slash Komutları Kontrolü (/zar, /yazitura, /rastgele, /yardim)
        if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.StartsWith("/"))
        {
            var parts = message.Content.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var rnd = new Random();

            if (cmd == "/zar")
            {
                var dice = rnd.Next(1, 7);
                message.Type = messageType.CommandResult;
                message.Content = $"🎲 {name} zar attı: {dice}!";
            }
            else if (cmd == "/yazitura")
            {
                var res = rnd.Next(0, 2) == 0 ? "Yazı 🪙" : "Tura 🦅";
                message.Type = messageType.CommandResult;
                message.Content = $"🪙 {name} yazı-tura attı: {res}!";
            }
            else if (cmd == "/rastgele")
            {
                int min = 1, max = 100;
                if (parts.Length > 1 && parts[1].Contains('-'))
                {
                    var rangeParts = parts[1].Split('-');
                    if (int.TryParse(rangeParts[0], out var pMin) && int.TryParse(rangeParts[1], out var pMax) && pMin < pMax)
                    {
                        min = pMin;
                        max = pMax;
                    }
                }
                else if (parts.Length > 1 && int.TryParse(parts[1], out var pMax) && pMax > 1)
                {
                    max = pMax;
                }
                var num = rnd.Next(min, max + 1);
                message.Type = messageType.CommandResult;
                message.Content = $"🎯 {name} rastgele sayı üretti ({min}-{max}): {num}!";
            }
            else if (cmd == "/xox")
            {
                await StartXoxGame(message.ReceiverId.ToString());
                return message;
            }
            else if (cmd == "/durt")
            {
                await SendBuzz(message.ReceiverId.ToString());
                return message;
            }
            else if (cmd == "/yardim")
            {
                message.Type = messageType.CommandResult;
                message.Content = "ℹ️ Kullanılabilir Komutlar:\n• /zar - 1-6 arası zar atar\n• /yazitura - Yazı/Tura atar\n• /rastgele 1-100 - Belirtilen aralıkta sayı üretir\n• /xox - XOX mini oyunu başlatır\n• /durt - Karşı tarafı dürter";
            }
        }

        await _messageService.AddMessageAsync(message);

        // Alıntı (Reply) bilgisi varsa alıcı ve göndericiye gönderilecek özeti hazırla
        object? replySummary = null;
        if (message.ReplyToMessageId.HasValue)
        {
            var replyTarget = _messageService.GetMessageById(message.ReplyToMessageId.Value);
            if (replyTarget != null)
            {
                replySummary = new
                {
                    Id = replyTarget.Id,
                    SenderId = replyTarget.SenderId,
                    SenderName = replyTarget.Sender?.Username ?? "Kullanıcı",
                    Content = replyTarget.IsDeleted ? "Bu mesaj silindi" : replyTarget.Content,
                    Type = replyTarget.Type,
                    FileUrl = replyTarget.FileUrl
                };
            }
        }

        // Alıcıya bildir
        await Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            Name = name,
            SenderId = senderId,
            Content = message,
            ReplyToMessageId = message.ReplyToMessageId,
            ReplyToMessage = replySummary,
            ExpiresAt = message.ExpiresAt,
            MetadataJson = message.MetadataJson,
            Timestamp = message.Timestamp
        });

        // Göndericiye onay dön (DB Id'si ve Reply bilgisi ile)
        await Clients.Caller.SendAsync("MessageSentAck", new
        {
            Id = message.Id,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            Content = message.Content,
            Timestamp = message.Timestamp,
            Status = message.Status,
            Type = message.Type,
            FileUrl = message.FileUrl,
            ReplyToMessageId = message.ReplyToMessageId,
            ReplyToMessage = replySummary,
            ExpiresAt = message.ExpiresAt,
            MetadataJson = message.MetadataJson
        });

        await sendNotification(message);
        return message;
    }

    public async Task SendWrite(string ReceiverId)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(senderId))
        {
            await Clients.User(ReceiverId).SendAsync("ReceiveWrites", senderId);
        }
    }

    public async Task SendSeen(string ReceiverId, messageStatus messageStatus)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(senderId))
        {
            await Clients.User(ReceiverId).SendAsync("ReceiveSeen", senderId, messageStatus);
            await _messageService.setSeen(int.Parse(ReceiverId), int.Parse(senderId), messageStatus);
        }
    }

    // Phase 4: Mesaj Düzenleme (SignalR)
    public async Task EditMessage(string ReceiverId, int messageId, string newContent)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId)) return;

        var updated = await _messageService.EditMessageAsync(messageId, int.Parse(senderId), newContent);
        if (updated != null)
        {
            var payload = new
            {
                Id = messageId,
                Content = newContent,
                EditedAt = updated.EditedAt
            };
            await Clients.User(ReceiverId).SendAsync("ReceiveEditMessage", payload);
            await Clients.Caller.SendAsync("ReceiveEditMessage", payload);
        }
    }

    // Phase 4: Mesaj Silme (Benden Sil / Herkesten Sil)
    public async Task DeleteMessage(string ReceiverId, int id, bool forEveryone = true)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId)) return;

        if (forEveryone)
        {
            await _messageService.DeleteMessageForEveryoneAsync(id, int.Parse(senderId));
            await Clients.User(ReceiverId).SendAsync("ReceiveDeleteMessage", senderId, id, true);
            await Clients.Caller.SendAsync("ReceiveDeleteMessage", senderId, id, true);
        }
        else
        {
            await _messageService.DeleteMessageForMeAsync(id, int.Parse(senderId));
            await Clients.Caller.SendAsync("ReceiveDeleteMessage", senderId, id, false);
        }
    }

    // Phase 4: Tepki (Reaction) Gönderme (SignalR)
    public async Task SendReaction(string ReceiverId, int messageId, string reaction)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId)) return;

        var reactions = await _messageService.SetReactionAsync(messageId, int.Parse(senderId), reaction);
        var payload = new
        {
            MessageId = messageId,
            UserId = int.Parse(senderId),
            Reaction = reaction,
            Reactions = reactions
        };
        await Clients.User(ReceiverId).SendAsync("ReceiveReaction", payload);
        await Clients.Caller.SendAsync("ReceiveReaction", payload);
    }

    // Phase 4: Sohbet Geçmişini Temizle (SignalR)
    public async Task ClearChat(string friendUserId)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId)) return;

        await _messageService.ClearChatAsync(int.Parse(senderId), int.Parse(friendUserId));
        await Clients.Caller.SendAsync("ReceiveClearChat", int.Parse(friendUserId));
    }

    // =========================================================================
    // Phase 5: Eğlence Özellikleri (Buzz / Nudge, XOX Mini Game, Self-Destruct)
    // =========================================================================

    // Phase 5: Titreşim / Dürtme (Buzz / Nudge)
    public async Task SendBuzz(string ReceiverId)
    {
        var senderIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var senderName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Bilinmeyen";
        if (string.IsNullOrEmpty(senderIdStr)) return;

        var senderId = int.Parse(senderIdStr);
        var receiverId = int.Parse(ReceiverId);

        if (_buzzCooldowns.TryGetValue(senderId, out var lastBuzzTime) && (DateTime.UtcNow - lastBuzzTime).TotalSeconds < 10)
        {
            var remaining = 10 - (int)(DateTime.UtcNow - lastBuzzTime).TotalSeconds;
            throw new HubException($"Tekrar dürtmek için {remaining} saniye beklemelisiniz.");
        }

        _buzzCooldowns[senderId] = DateTime.UtcNow;

        var buzzMessage = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = $"⚡ {senderName} seni dürttü! 📳",
            Type = messageType.Buzz,
            Timestamp = DateTime.UtcNow
        };

        await _messageService.AddMessageAsync(buzzMessage);

        var payload = new
        {
            Id = buzzMessage.Id,
            SenderId = senderId,
            ReceiverId = receiverId,
            SenderName = senderName,
            Content = buzzMessage.Content,
            Timestamp = buzzMessage.Timestamp,
            Type = buzzMessage.Type
        };

        await Clients.User(ReceiverId).SendAsync("ReceiveBuzz", payload);
        await Clients.Caller.SendAsync("BuzzSentAck", payload);

        await sendNotification(buzzMessage);
    }

    // Phase 5: XOX Mini Oyun Başlat
    public async Task StartXoxGame(string ReceiverId)
    {
        var senderIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var senderName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Oyuncu 1";
        if (string.IsNullOrEmpty(senderIdStr)) return;

        var senderId = int.Parse(senderIdStr);
        var receiverId = int.Parse(ReceiverId);

        var gameMessage = await _messageService.StartXoxGameAsync(senderId, receiverId, senderName, "Arkadaşınız");

        var payload = new
        {
            Id = gameMessage.Id,
            Name = senderName,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = gameMessage,
            MetadataJson = gameMessage.MetadataJson,
            Timestamp = gameMessage.Timestamp,
            Type = gameMessage.Type
        };

        await Clients.User(ReceiverId).SendAsync("ReceiveMessage", payload);
        await Clients.Caller.SendAsync("MessageSentAck", payload);
    }

    // Phase 5: XOX Hamlesi Yap
    public async Task MakeXoxMove(int messageId, int cellIndex)
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr)) return;

        var userId = int.Parse(userIdStr);
        try
        {
            var (message, state) = await _messageService.MakeXoxMoveAsync(messageId, userId, cellIndex);

            var updatePayload = new
            {
                MessageId = messageId,
                Content = message.Content,
                MetadataJson = message.MetadataJson,
                State = state
            };

            await Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveGameUpdate", updatePayload);
            await Clients.User(message.SenderId.ToString()).SendAsync("ReceiveGameUpdate", updatePayload);
        }
        catch (Exception ex)
        {
            throw new HubException(ex.Message);
        }
    }

    // Phase 5: Kendini İmha Eden Mesaj Süre Sonu
    public async Task ExpireMessage(int messageId)
    {
        var msg = _messageService.GetMessageById(messageId);
        if (msg == null) return;

        await _messageService.ExpireMessageAsync(messageId);

        var payload = new
        {
            MessageId = messageId,
            Content = "🔥 Bu mesajın süresi doldu."
        };

        await Clients.User(msg.ReceiverId.ToString()).SendAsync("ReceiveMessageExpired", payload);
        await Clients.Caller.SendAsync("ReceiveMessageExpired", payload);
    }

    private async Task sendNotification(Message message)
    {
        var name = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Bilinmeyen";
        bool isOnline = _onlineUsers.Values.Contains(message.ReceiverId.ToString());
        if (isOnline) return;
        await _fbTokenService.SendNotificationToUserAsync(message.ReceiverId, message.Content, name, message.SenderId.ToString());
    }
}
