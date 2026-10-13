using System.ComponentModel.DataAnnotations;

namespace TimeVerse.ViewModels.Admin
{
    public class CreateSalesPersonViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Corporate / Sales Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Contact Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Temporary password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Initial Temporary Password")]
        public string TemporaryPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(nameof(TemporaryPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool Succeeded { get; set; } = false;
        public string? Message { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalWatches { get; set; } = 0;
        public int TotalCustomers { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;
        public decimal TotalRevenue { get; set; } = 0.00m;
        public int LowStockCount { get; set; } = 0;
        public int PendingOrdersCount { get; set; } = 0;

        public List<RecentOrderAdminViewModel> RecentOrders { get; set; } = new();
        public List<LowStockWatchAdminViewModel> LowStockWatches { get; set; } = new();
        public List<TopSellingWatchAdminViewModel> TopSellingWatches { get; set; } = new();
    }

    public class RecentOrderAdminViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemsCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class LowStockWatchAdminViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class TopSellingWatchAdminViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int UnitsSold { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
