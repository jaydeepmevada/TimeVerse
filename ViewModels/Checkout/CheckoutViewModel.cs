using System.ComponentModel.DataAnnotations;
using TimeVerse.Models;
using TimeVerse.ViewModels.Cart;
using TimeVerse.ViewModels.Customer;

namespace TimeVerse.ViewModels.Checkout
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();

        // Selected saved address ID (optional)
        public int? SelectedAddressId { get; set; }
        public List<AddressViewModel> SavedAddresses { get; set; } = new();

        // New Address Fields
        [Required(ErrorMessage = "Recipient full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(200)]
        [Display(Name = "Address Line 1")]
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Apartment / Suite (Optional)")]
        public string? ShippingAddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        [Display(Name = "State")]
        public string ShippingState { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal/Pincode is required.")]
        [StringLength(20)]
        [Display(Name = "PIN / Postal Code")]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        [Display(Name = "Country")]
        public string ShippingCountry { get; set; } = "India";

        [Required(ErrorMessage = "Contact telephone is required.")]
        [Phone]
        [Display(Name = "Mobile Phone")]
        public string ShippingPhone { get; set; } = string.Empty;

        [Display(Name = "Save as default delivery destination")]
        public bool SaveToAddressBook { get; set; } = false;

        // Payment Method
        [Required(ErrorMessage = "Please select a payment method.")]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UPI;

        [Display(Name = "Vault Dispatch Instructions / Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class OrderConfirmationViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal FinalAmount { get; set; }
        public string ShippingFullName { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public OrderStatus Status { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();
    }
}
