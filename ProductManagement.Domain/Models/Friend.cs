using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Domain.Models
{
    public class Friend
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual Users? User { get; set; }

        public int FriendUserId { get; set; }
        public virtual Users? FriendUser { get; set; }

        public DateTime FriendsSince { get; set; } = DateTime.UtcNow;

        public bool IsPinned { get; set; } = false;

        public bool IsMuted { get; set; } = false;

        public bool IsArchived { get; set; } = false;
    }
}
