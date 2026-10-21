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

        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }
    }

    public enum FriendRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2,
        Canceled = 3,
        Blocked = 4
    }
}
