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

    public class RpsGameState
    {
        public string GameType { get; set; } = "rps";
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public string? Player1Choice { get; set; }
        public string? Player2Choice { get; set; }
        public int Player1Score { get; set; } = 0;
        public int Player2Score { get; set; } = 0;
        public int Round { get; set; } = 1;
        public int TargetScore { get; set; } = 3;
        public string? LastRoundPlayer1Choice { get; set; }
        public string? LastRoundPlayer2Choice { get; set; }
        public string? LastRoundSummary { get; set; }
        public string? Winner { get; set; }
        public string Status { get; set; } = "playing";
    }
}
