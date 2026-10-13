namespace TimeVerse.ViewModels.Cart
{
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public int WatchId { get; set; }
        public string WatchName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public int StockQuantity
        {
            get => AvailableStock;
            set => AvailableStock = value;
        }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public int TotalItemCount => Items.Sum(i => i.Quantity);
        public decimal Subtotal => Items.Sum(i => i.TotalPrice);
        public decimal FreeShippingThreshold { get; set; } = 50000.00m;
        public decimal ShippingFee => Subtotal >= FreeShippingThreshold || Subtotal == 0 ? 0 : 1500.00m;
        public decimal GrandTotal => Subtotal + ShippingFee;
        public bool QualifiesForFreeShipping => Subtotal >= FreeShippingThreshold;
        public decimal AmountNeededForFreeShipping => Math.Max(0, FreeShippingThreshold - Subtotal);
    }
}
