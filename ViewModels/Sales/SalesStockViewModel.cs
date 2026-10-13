namespace TimeVerse.ViewModels.Sales
{
    public class SalesStockItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsLowStock => StockQuantity <= LowStockThreshold && StockQuantity > 0;
        public bool IsOutOfStock => StockQuantity == 0;
    }

    public class SalesStockListViewModel
    {
        public List<SalesStockItemViewModel> Watches { get; set; } = new();
        public string? Search { get; set; }
        public string? StockFilter { get; set; }
        public int TotalWatchesCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }
}
