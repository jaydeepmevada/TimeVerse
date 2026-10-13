using System.ComponentModel.DataAnnotations;
using TimeVerse.Models;

namespace TimeVerse.DTOs
{
    public class WatchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock => StockQuantity > 0;
        public MovementType MovementType { get; set; }
        public WatchGender Gender { get; set; }
        public string CaseSize { get; set; } = string.Empty;
        public string CaseMaterial { get; set; } = string.Empty;
        public string DialColor { get; set; } = string.Empty;
        public string StrapMaterial { get; set; } = string.Empty;
        public string WaterResistance { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsLimitedEdition { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
    }

    public class WatchCreateDto
    {
        [Required(ErrorMessage = "Watch name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU is required.")]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        public int BrandId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Range(1, 100000000)]
        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }
        public decimal DiscountPercentage { get; set; }

        [Range(0, 10000)]
        public int StockQuantity { get; set; } = 1;

        public MovementType MovementType { get; set; }
        public WatchGender Gender { get; set; }
        public string CaseSize { get; set; } = "40mm";
        public string CaseMaterial { get; set; } = "Stainless Steel";
        public string DialColor { get; set; } = "Black";
        public string StrapMaterial { get; set; } = "Leather";
        public string WaterResistance { get; set; } = "50m / 5 ATM";
        public string Warranty { get; set; } = "2 Years International";
        public string CountryOfOrigin { get; set; } = "Switzerland";
        public string Description { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsLimitedEdition { get; set; }
    }

    public class WatchUpdateDto : WatchCreateDto
    {
        public bool IsActive { get; set; } = true;
    }

    public class WatchListResponseDto
    {
        public List<WatchDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
