using System.Xml;
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
        public Users Sender { get; set; }

        public int ReceiverId { get; set; }
        public Users Receiver { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public messageStatus Status { get; set; }
        public messageType Type { get; set; }
        public string? FileUrl { get; set; }
        public bool IsDeleted { get; set; } = false;

    }

    public enum messageStatus
    {
        Sent,
        forwarded,
        Seen
    }

    public enum messageType
    {
        Text,
        Image,
        File,
        Audio,
        Video

    }
}

