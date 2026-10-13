namespace TimeVerse.ViewModels.Wishlist
{
    public class WishlistItemViewModel
    {
        public int Id { get; set; }
        public int WatchId { get; set; }
        public string WatchName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool InStock => StockQuantity > 0;
        public DateTime AddedDate { get; set; }
    }

    public class WishlistViewModel
    {
        public List<WishlistItemViewModel> Items { get; set; } = new();
        public int TotalCount => Items.Count;
    }
}
