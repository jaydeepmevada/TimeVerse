using System.ComponentModel.DataAnnotations;

namespace TimeVerse.DTOs
{
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int WatchCount { get; set; }
    }

    public class BrandCreateDto
    {
        [Required(ErrorMessage = "Brand name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
