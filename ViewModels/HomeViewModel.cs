using TimeVerse.Models;

namespace TimeVerse.ViewModels
{
    public class CustomerReviewItem
    {
        public string AuthorName { get; set; } = string.Empty;
        public string WatchName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public string DateAgo { get; set; } = string.Empty;
        public bool IsVerifiedPurchaser { get; set; } = true;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class CategorySpotlightItem
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class HomeViewModel
    {
        public List<WatchCardViewModel> FeaturedWatches { get; set; } = new();
        public List<WatchCardViewModel> NewArrivals { get; set; } = new();
        public List<WatchCardViewModel> BestSellers { get; set; } = new();
        public List<WatchCardViewModel> FeaturedCollection { get; set; } = new();
        public List<WatchCardViewModel> LimitedEditions { get; set; } = new();
        public List<CategorySpotlightItem> Categories { get; set; } = new();
        public List<CustomerReviewItem> Reviews { get; set; } = new();
        public List<string> PopularBrands { get; set; } = new()
        {
            "Rolex", "Omega", "Seiko", "Casio", "Titan", "Fossil", "Tissot", "Citizen"
        };
    }
}
