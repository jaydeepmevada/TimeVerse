using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeVerse.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UPI;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // Shipping Destination Snapshot
        [Required]
        [StringLength(100)]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ShippingAddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ShippingState { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ShippingCountry { get; set; } = "India";

        [Required]
        [StringLength(20)]
        public string ShippingPhone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
