using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Domain.Models
{
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SenderId { get; set; }
        public virtual Users? Sender { get; set; }

        public int ReceiverId { get; set; }
        public virtual Users? Receiver { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public messageStatus Status { get; set; } = messageStatus.Sent;

        public messageType Type { get; set; } = messageType.Text;

        public string? FileUrl { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsDeletedForSender { get; set; } = false;

        public bool IsDeletedForReceiver { get; set; } = false;

        public int? ReplyToMessageId { get; set; }
        public virtual Message? ReplyToMessage { get; set; }

        public DateTime? EditedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public string? MetadataJson { get; set; }
    }

    public enum messageStatus
    {
        Sent = 0,
        forwarded = 1,
        Seen = 2
    }

    public enum messageType
    {
        Text = 0,
        Image = 1,
        File = 2,
        Audio = 3,
        Video = 4,
        Buzz = 5,
        Game = 6,
        CommandResult = 7
    }
}
