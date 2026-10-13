using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeVerse.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(100)]
        public string? SessionId { get; set; }

        [Required]
        public int WatchId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual Watch? Watch { get; set; }
    }
}
