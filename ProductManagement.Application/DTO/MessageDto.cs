using ProductManagement.Domain.Models;

namespace ProductManagement.Application.DTO
{
    public class MessageDto
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ReplyMessageSummaryDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public messageType Type { get; set; }
        public string? FileUrl { get; set; }
    }

    public class MessageResponseDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public messageStatus Status { get; set; }
        public messageType Type { get; set; }
        public string? FileUrl { get; set; }
        public bool IsDeleted { get; set; }
        public int? ReplyToMessageId { get; set; }
        public ReplyMessageSummaryDto? ReplyToMessage { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? MetadataJson { get; set; }
        public Dictionary<string, string> Reactions { get; set; } = new Dictionary<string, string>();
    }

    public class MessageEditDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class MessageReactionDto
    {
        public string Reaction { get; set; } = string.Empty;
    }

    public class XoxGameState
    {
        public string GameType { get; set; } = "xox";
        public int PlayerX { get; set; }
        public int PlayerO { get; set; }
        public string PlayerXName { get; set; } = string.Empty;
        public string PlayerOName { get; set; } = string.Empty;
        public string[] Board { get; set; } = new string[9] { "", "", "", "", "", "", "", "", "" };
        public string Turn { get; set; } = "X";
        public string? Winner { get; set; }
        public string Status { get; set; } = "playing";
        public int[]? WinningLine { get; set; }
    }
}
