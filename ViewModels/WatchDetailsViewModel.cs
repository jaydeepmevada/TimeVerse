using TimeVerse.Models;

namespace TimeVerse.ViewModels
{
    public class WatchDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock => StockQuantity > 0;
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 5;
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public string Description { get; set; } = string.Empty;

        public MovementType MovementType { get; set; }
        public WatchGender Gender { get; set; }
        public string CaseSize { get; set; } = string.Empty;
        public string CaseMaterial { get; set; } = string.Empty;
        public string DialColor { get; set; } = string.Empty;
        public string StrapMaterial { get; set; } = string.Empty;
        public string WaterResistance { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;

        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsLimitedEdition { get; set; }

        public List<string> Images { get; set; } = new();
        public string PrimaryImageUrl => Images.FirstOrDefault() ?? "/images/watches/placeholder.svg";

        public bool CanUserReview { get; set; }
        public bool IsVerifiedPurchaser { get; set; }

        public List<WatchCardViewModel> RelatedWatches { get; set; } = new();
        public List<CustomerReviewItem> CustomerReviews { get; set; } = new();
    }
}
