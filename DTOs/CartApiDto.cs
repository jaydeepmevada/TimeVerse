using System.ComponentModel.DataAnnotations;
using TimeVerse.Models;

namespace TimeVerse.DTOs
{
    public class CartSummaryDto
    {
        public int TotalItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal GrandTotal { get; set; }
        public bool QualifiesForFreeShipping { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public int WatchId { get; set; }
        public string WatchName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CartItemAddDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int WatchId { get; set; }

        [Range(1, 10)]
        public int Quantity { get; set; } = 1;
    }

    public class CartItemUpdateDto
    {
        [Range(0, 50)]
        public int Quantity { get; set; }
    }

    public class OrderTrackingResponseDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public string? TrackingNumber { get; set; }
        public decimal FinalAmount { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }
}
