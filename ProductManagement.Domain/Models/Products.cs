using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProductManagement.Domain.Models
{
    public class Products
    {
        [Key]  // Birincil anahtar olduğunu belirtir
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }

        public decimal Price { get; set; }

        public int CategoryId { get; set; }

        public virtual Category? Category { get; set; }
    }
}
