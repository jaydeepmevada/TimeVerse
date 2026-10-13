using System.ComponentModel.DataAnnotations;

namespace TimeVerse.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int WatchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual Watch? Watch { get; set; }
    }
}
