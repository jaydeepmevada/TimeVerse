using TimeVerse.Models;

namespace TimeVerse.ViewModels.Orders
{
    public class OrderLineItemViewModel
    {
        public int Id { get; set; }
        public int WatchId { get; set; }
        public string WatchName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string WatchSKU => SKU;
        public string BrandName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class OrderSummaryViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal TotalAmount => FinalAmount;
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public int ItemCount { get; set; }
        public int TotalItems => ItemCount;
    }

    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotal => TotalAmount;
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal TaxAmount => Math.Round(FinalAmount * 0.18m / 1.18m, 2);
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingEmail { get; set; } = string.Empty;
        public string ShippingAddressLine1 { get; set; } = string.Empty;
        public string? ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingPostalCode { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;

        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }

        public List<OrderLineItemViewModel> Items { get; set; } = new();
    }
}
