using System.ComponentModel.DataAnnotations;

namespace TimeVerse.Models
{
    public class Testimonial
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Review text is required.")]
        [StringLength(1000)]
        public string Review { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(255)]
        public string ImageUrl { get; set; } = "/images/avatars/avatar-1.svg";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
