using System.ComponentModel.DataAnnotations;

namespace TimeVerse.Models
{
    public class WatchImage
    {
        public int Id { get; set; }

        public int WatchId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Watch? Watch { get; set; }
    }
}
