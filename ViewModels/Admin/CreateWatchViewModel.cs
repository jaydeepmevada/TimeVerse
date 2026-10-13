using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TimeVerse.Models;

namespace TimeVerse.ViewModels.Admin
{
    public class CreateWatchViewModel
    {
        // 1. Watch Name
        [Required(ErrorMessage = "Watch name is required.")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        [Display(Name = "Watch Name")]
        public string Name { get; set; } = string.Empty;

        // Auto-generated if not provided by user
        [Display(Name = "SKU Reference")]
        public string? SKU { get; set; }

        // 2. Brand
        [Required(ErrorMessage = "Please select a brand.")]
        [Display(Name = "Brand")]
        public int BrandId { get; set; }

        // 3. Category
        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // 4. Price (MRP)
        [Required(ErrorMessage = "Price (MRP) is required.")]
        [Range(1, 100000000, ErrorMessage = "Price (MRP) must be an integer greater than zero.")]
        [Display(Name = "Price (MRP)")]
        public int? OriginalPrice { get; set; }

        // 5. Selling Price
        [Required(ErrorMessage = "Selling price is required.")]
        [Range(1, 100000000, ErrorMessage = "Selling price must be an integer greater than zero.")]
        [Display(Name = "Selling Price")]
        public int Price { get; set; }

        [Display(Name = "Discount (%)")]
        public decimal DiscountPercentage { get; set; } = 0;

        // 6. Description
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(3000, ErrorMessage = "Description cannot exceed 3000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        // 7. Watch Image
        [Display(Name = "Watch Image")]
        public List<IFormFile>? Images { get; set; }

        // 8. Stock Quantity
        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, 1000000, ErrorMessage = "Stock cannot be negative.")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; } = 10;

        // 9. Status — Active / Inactive
        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        // Technical defaults to satisfy database models without cluttering form
        public int LowStockThreshold { get; set; } = 5;
        public MovementType MovementType { get; set; } = MovementType.Automatic;
        public WatchGender Gender { get; set; } = WatchGender.Unisex;
        public string CaseSize { get; set; } = "40mm";
        public string CaseMaterial { get; set; } = "Stainless Steel";
        public string DialColor { get; set; } = "Black";
        public string StrapMaterial { get; set; } = "Leather";
        public string WaterResistance { get; set; } = "100m / 10 ATM";
        public string Warranty { get; set; } = "2 Years International";
        public string CountryOfOrigin { get; set; } = "Switzerland";
        public bool IsFeatured { get; set; } = false;
        public bool IsBestSeller { get; set; } = false;
        public bool IsNewArrival { get; set; } = true;
        public bool IsLimitedEdition { get; set; } = false;

        // Select lists
        public List<SelectListItem> BrandList { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();
    }
}
