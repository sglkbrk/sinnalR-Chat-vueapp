using System.Xml;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Domain.Models
{
    public class FbToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public required int UserId { get; set; }
        public virtual Users? User { get; set; }

        public required string Token { get; set; }


    }
}