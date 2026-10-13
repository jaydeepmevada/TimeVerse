using TimeVerse.ViewModels.Orders;

namespace TimeVerse.ViewModels.Admin
{
    public class AdminUserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUserListViewModel
    {
        public List<AdminUserItemViewModel> Users { get; set; } = new();
        public string? Search { get; set; }
        public string? RoleFilter { get; set; }
        public int TotalUsersCount { get; set; }
    }

    public class AdminUserOrderHistoryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderSummaryViewModel> Orders { get; set; } = new();
    }
}
