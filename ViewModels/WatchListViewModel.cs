using TimeVerse.Models;

namespace TimeVerse.ViewModels
{
    public class WatchFilterOptions
    {
        public List<string> Brands { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<MovementType> Movements { get; set; } = new();
        public List<WatchGender> Genders { get; set; } = new();
        public decimal MinPriceAvailable { get; set; }
        public decimal MaxPriceAvailable { get; set; }
    }

    public class WatchListViewModel
    {
        public List<WatchCardViewModel> Watches { get; set; } = new();

        // Search and Filters
        public string? Search { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public MovementType? Movement { get; set; }
        public WatchGender? Gender { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? StockFilter { get; set; } // "all", "inStock", "outOfStock"
        public string Sort { get; set; } = "featured";

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 9));
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Filter master lists
        public WatchFilterOptions FilterOptions { get; set; } = new();
    }
}
