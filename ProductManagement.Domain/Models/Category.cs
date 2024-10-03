using System.Xml;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Domain.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public required int Id { get; set; }

        public required string Name { get; set; }

        // public virtual   ICollection<Product> Products { get; set; }
    }
}