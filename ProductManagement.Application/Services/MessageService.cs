using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using ProductManagement.Application.DTO;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ProductManagement.Application.Services
{
    public class MessageMetadata
    {
        public Dictionary<string, string> Reactions { get; set; } = new Dictionary<string, string>();
    }

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

        public async Task<List<MessageResponseDto>> GetMyMessagesAsync(int senderId, int receiverId, int page = 1, int pageSize = 50)
        {
            // Filtreleme: Kullanıcının "Benden Sil" dediği mesajları hariç tut
            var rawMessages = await _context.Message.AsNoTracking()
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Sender)
                .Where(m =>
                    (m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsDeletedForSender) ||
                    (m.SenderId == receiverId && m.ReceiverId == senderId && !m.IsDeletedForReceiver))
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            rawMessages.Reverse();

            return rawMessages.Select(m => MapToResponseDto(m)).ToList();
        }

        public MessageResponseDto MapToResponseDto(Message m)
        {
            ReplyMessageSummaryDto? replySummary = null;
            if (m.ReplyToMessage != null)
            {
                replySummary = new ReplyMessageSummaryDto
                {
                    Id = m.ReplyToMessage.Id,
                    SenderId = m.ReplyToMessage.SenderId,
                    SenderName = m.ReplyToMessage.Sender?.Username ?? "Kullanıcı",
                    Content = m.ReplyToMessage.IsDeleted ? "Bu mesaj silindi" : m.ReplyToMessage.Content,
                    Type = m.ReplyToMessage.Type,
                    FileUrl = m.ReplyToMessage.FileUrl
                };
            }

            return new MessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                Status = m.Status,
                Type = m.Type,
                FileUrl = m.FileUrl,
                IsDeleted = m.IsDeleted,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = replySummary,
                EditedAt = m.EditedAt,
                ExpiresAt = m.ExpiresAt,
                MetadataJson = m.MetadataJson,
                Reactions = ParseReactions(m.MetadataJson)
            };
        }

        public static Dictionary<string, string> ParseReactions(string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(metadataJson)) return new Dictionary<string, string>();
            try
            {
                var meta = JsonSerializer.Deserialize<MessageMetadata>(metadataJson);
                return meta?.Reactions ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public Message? GetMessageById(int id)
        {
            return _context.Message
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.Sender)
                .FirstOrDefault(c => c.Id == id);
        }

        public async Task AddMessageAsync(Message message)
        {
            _context.Message.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMessage(Message message)
        {
            _context.Message.Update(message);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMessage(int id)
        {
            var message = await _context.Message.FindAsync(id);
            if (message != null)
            {
                _context.Message.Remove(message);
                await _context.SaveChangesAsync();
            }
        }

        // Phase 4: Düzenleme
        public async Task<Message?> EditMessageAsync(int messageId, int userId, string newContent)
        {
            var message = await _context.Message.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return null;

            if (message.SenderId != userId)
                throw new UnauthorizedAccessException("Yalnızca kendi mesajlarınızı düzenleyebilirsiniz.");

            if (message.IsDeleted)
                throw new InvalidOperationException("Silinmiş mesajlar düzenlenemez.");

            message.Content = newContent;
            message.EditedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return message;
        }

        // Phase 4: Benden Sil (Delete for me)
        public async Task<bool> DeleteMessageForMeAsync(int messageId, int userId)
        {
            var message = await _context.Message.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return false;

            if (message.SenderId == userId)
            {
                message.IsDeletedForSender = true;
            }
            else if (message.ReceiverId == userId)
            {
                message.IsDeletedForReceiver = true;
            }
            else
            {
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Phase 4: Herkesten Sil (Delete for everyone)
        public async Task<bool> DeleteMessageForEveryoneAsync(int messageId, int userId)
        {
            var message = await _context.Message.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return false;

            if (message.SenderId != userId)
                throw new UnauthorizedAccessException("Yalnızca kendi mesajlarınızı herkesten silebilirsiniz.");

            message.IsDeleted = true;
            message.Content = "Bu mesaj silindi";
            await _context.SaveChangesAsync();
            return true;
        }

        // Phase 4: Sohbet Geçmişini Temizle (Clear Chat History)
        public async Task<bool> ClearChatAsync(int currentUserId, int friendUserId)
        {
            var messages = await _context.Message
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendUserId) ||
                            (m.SenderId == friendUserId && m.ReceiverId == currentUserId))
                .ToListAsync();

            foreach (var msg in messages)
            {
                if (msg.SenderId == currentUserId)
                {
                    msg.IsDeletedForSender = true;
                }
                if (msg.ReceiverId == currentUserId)
                {
                    msg.IsDeletedForReceiver = true;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Phase 4: Tepki (Reaction) Ekle / Değiştir / Kaldır (Toggle)
        public async Task<Dictionary<string, string>> SetReactionAsync(int messageId, int userId, string reactionEmoji)
        {
            var message = await _context.Message.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return new Dictionary<string, string>();

            MessageMetadata metadata;
            try
            {
                metadata = string.IsNullOrWhiteSpace(message.MetadataJson)
                    ? new MessageMetadata()
                    : JsonSerializer.Deserialize<MessageMetadata>(message.MetadataJson) ?? new MessageMetadata();
            }
            catch
            {
                metadata = new MessageMetadata();
            }

            var userKey = userId.ToString();
            // Boş reaction veya aynı emojiye tekrar basıldığında kaldır
            if (string.IsNullOrWhiteSpace(reactionEmoji) ||
                (metadata.Reactions.TryGetValue(userKey, out var existing) && existing == reactionEmoji))
            {
                metadata.Reactions.Remove(userKey);
            }
            else
            {
                metadata.Reactions[userKey] = reactionEmoji;
            }

            message.MetadataJson = JsonSerializer.Serialize(metadata);
            await _context.SaveChangesAsync();
            return metadata.Reactions;
        }

        // Phase 5: XOX Oyunu Başlat
        public async Task<Message> StartXoxGameAsync(int senderId, int receiverId, string senderName, string receiverName)
        {
            var gameState = new XoxGameState
            {
                GameType = "xox",
                PlayerX = senderId,
                PlayerO = receiverId,
                PlayerXName = senderName,
                PlayerOName = receiverName,
                Board = new string[9] { "", "", "", "", "", "", "", "", "" },
                Turn = "X",
                Status = "playing"
            };

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = $"🎮 XOX: {senderName} vs {receiverName}",
                Type = messageType.Game,
                Timestamp = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(gameState)
            };

            _context.Message.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        // Phase 5: XOX Hamlesi Yap
        public async Task<(Message message, XoxGameState state)> MakeXoxMoveAsync(int messageId, int userId, int cellIndex)
        {
            var message = await _context.Message.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) throw new KeyNotFoundException("Oyun mesajı bulunamadı.");
            if (message.Type != messageType.Game) throw new InvalidOperationException("Bu mesaj bir oyun mesajı değil.");

            var state = JsonSerializer.Deserialize<XoxGameState>(message.MetadataJson ?? "{}") ?? new XoxGameState();
            if (state.Status != "playing") throw new InvalidOperationException("Bu oyun zaten sonuçlanmış.");

            if (cellIndex < 0 || cellIndex > 8) throw new ArgumentOutOfRangeException(nameof(cellIndex), "Geçersiz kare indeksi.");
            if (!string.IsNullOrEmpty(state.Board[cellIndex])) throw new InvalidOperationException("Bu kare zaten dolu.");

            string playerSymbol;
            if (userId == state.PlayerX) playerSymbol = "X";
            else if (userId == state.PlayerO) playerSymbol = "O";
            else throw new UnauthorizedAccessException("Bu oyunun oyuncusu değilsiniz.");

            if (state.Turn != playerSymbol) throw new InvalidOperationException("Hamle sırası sizde değil.");

            // Hamleyi işle
            state.Board[cellIndex] = playerSymbol;

            // Kazanma durumunu kontrol et
            int[][] winLines = new int[][]
            {
                new int[] { 0, 1, 2 }, new int[] { 3, 4, 5 }, new int[] { 6, 7, 8 }, // Satırlar
                new int[] { 0, 3, 6 }, new int[] { 1, 4, 7 }, new int[] { 2, 5, 8 }, // Sütunlar
                new int[] { 0, 4, 8 }, new int[] { 2, 4, 6 }                          // Çaprazlar
            };

            bool won = false;
            foreach (var line in winLines)
            {
                if (state.Board[line[0]] == playerSymbol &&
                    state.Board[line[1]] == playerSymbol &&
                    state.Board[line[2]] == playerSymbol)
                {
                    won = true;
                    state.Winner = playerSymbol;
                    state.Status = "finished";
                    state.WinningLine = line;
                    var winnerName = playerSymbol == "X" ? state.PlayerXName : state.PlayerOName;
                    message.Content = $"🏆 XOX: {winnerName} kazandı!";
                    break;
                }
            }

            if (!won)
            {
                // Beraberlik kontrolü
                bool isFull = state.Board.All(cell => !string.IsNullOrEmpty(cell));
                if (isFull)
                {
                    state.Winner = "Draw";
                    state.Status = "finished";
                    message.Content = "🤝 XOX: Oyun berabere bitti!";
                }
                else
                {
                    // Sırayı diğer oyuncuya geçir
                    state.Turn = state.Turn == "X" ? "O" : "X";
                }
            }

            message.MetadataJson = JsonSerializer.Serialize(state);
            await _context.SaveChangesAsync();
            return (message, state);
        }

        // Phase 5: Kendini İmha Eden Mesaj Süre Sonu
        public async Task<bool> ExpireMessageAsync(int messageId)
        {
            var message = await _context.Message.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return false;

            message.IsDeleted = true;
            message.Content = "🔥 Bu mesajın süresi doldu.";
            await _context.SaveChangesAsync();
            return true;
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

            await _context.SaveChangesAsync();
        }
    }
}
