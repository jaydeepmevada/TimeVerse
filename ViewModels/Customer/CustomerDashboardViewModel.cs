using System.ComponentModel.DataAnnotations;
using TimeVerse.ViewModels;

namespace TimeVerse.ViewModels.Customer
{
    public class AddressViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Recipient full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street address line 1 is required.")]
        [StringLength(200)]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Apartment, suite, unit (optional)")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        [Display(Name = "Country")]
        public string Country { get; set; } = "India";

        [Required(ErrorMessage = "Postal/Pincode is required.")]
        [StringLength(20)]
        [Display(Name = "PIN / Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact phone number is required.")]
        [Phone]
        [Display(Name = "Contact Phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Set as default shipping address")]
        public bool IsDefault { get; set; } = false;
    }

    public class CustomerDashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OrderCount { get; set; } = 0;
        public int WishlistCount { get; set; } = 0;
        public int CartCount { get; set; } = 0;
        public int AddressCount { get; set; } = 0;
        public bool IsNewUser { get; set; } = false;
        public List<AddressViewModel> Addresses { get; set; } = new();
    }
}
