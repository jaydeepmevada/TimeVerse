using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeVerse.Models
{
    public class Watch
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(3000, ErrorMessage = "Description cannot exceed 3000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int BrandId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 100000000, ErrorMessage = "Price must be greater than zero")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Range(0, 100000000, ErrorMessage = "Original price cannot be negative")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100%")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0;

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 1000000, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; } = 0;

        [Range(0, 1000, ErrorMessage = "Low stock threshold cannot be negative")]
        public int LowStockThreshold { get; set; } = 5;

        public MovementType MovementType { get; set; } = MovementType.Automatic;

        public WatchGender Gender { get; set; } = WatchGender.Unisex;

        [StringLength(50)]
        public string CaseSize { get; set; } = "40mm";

        [StringLength(100)]
        public string CaseMaterial { get; set; } = "Stainless Steel";

        [StringLength(50)]
        public string DialColor { get; set; } = "Black";

        [StringLength(100)]
        public string StrapMaterial { get; set; } = "Leather";

        [StringLength(50)]
        public string WaterResistance { get; set; } = "100m / 10 ATM";

        [StringLength(100)]
        public string Warranty { get; set; } = "2 Years International";

        [StringLength(100)]
        public string CountryOfOrigin { get; set; } = "Switzerland";

        [Range(0, 5)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 5.0m;

        public int ReviewCount { get; set; } = 0;

        public bool IsFeatured { get; set; } = false;

        public bool IsBestSeller { get; set; } = false;

        public bool IsNewArrival { get; set; } = false;

        public bool IsLimitedEdition { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Brand? Brand { get; set; }

        public virtual Category? Category { get; set; }

        public virtual ICollection<WatchImage> Images { get; set; } = new List<WatchImage>();
    }
}
