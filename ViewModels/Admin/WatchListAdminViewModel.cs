using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TimeVerse.Models;

namespace TimeVerse.ViewModels.Admin
{
    public class WatchAdminItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class WatchListAdminViewModel
    {
        public List<WatchAdminItemViewModel> Watches { get; set; } = new();
        public string? Search { get; set; }
        public string? Brand { get; set; }
        public string? StockFilter { get; set; }
    }

    public class EditWatchViewModel : CreateWatchViewModel
    {
        public int Id { get; set; }
        public List<string> ExistingImages { get; set; } = new();
    }

    public class BrandCrudViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Brand name is required.")]
        [StringLength(100)]
        [Display(Name = "Brand Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int WatchCount { get; set; }
    }

    public class CategoryCrudViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Category Taxonomy Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int WatchCount { get; set; }
    }
}
