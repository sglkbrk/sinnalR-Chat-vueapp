using System.Xml;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Domain.Models
{
    public class FriendRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SenderId { get; set; }
        public virtual Users? Sender { get; set; }
        public int ReceiverId { get; set; }
        public virtual Users? Receiver { get; set; }
        public FriendRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? RespondedAt { get; set; }
    }
    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Declined
    }
}