using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Domain.Models
{
    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçersiz email adresi.")]
        [MaxLength(100)]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir.")]
        public required string Password { get; set; }

        public string? AvatarUrl { get; set; }

        [MaxLength(200)]
        public string? StatusMessage { get; set; }

        [MaxLength(20)]
        public string? UserCode { get; set; }

        public DateTime? LastSeenAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
