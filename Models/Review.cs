using System.ComponentModel.DataAnnotations;

namespace TimeVerse.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int WatchId { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please enter your review comments.")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Review must be between 5 and 1000 characters.")]
        public string Comment { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = true;

        public bool IsVerifiedPurchase { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Watch? Watch { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
