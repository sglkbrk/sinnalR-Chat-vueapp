using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



public class Users
{
    [Key]  // Birincil anahtar olduğunu belirtir
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id { get; set; }
    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    public required string Username { get; set; }
    [Required(ErrorMessage = "Email gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçersiz email adresi.")]
    public required string Email { get; set; }
    [Required(ErrorMessage = "Şifre gereklidir.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public required string Password { get; set; }


}
