using TimeVerse.Models;

namespace TimeVerse.ViewModels
{
    public class WatchCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public MovementType MovementType { get; set; }
        public WatchGender Gender { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock => StockQuantity > 0;
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 5;
        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsLimitedEdition { get; set; }
    }
}
