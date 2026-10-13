using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.ViewModels.Admin;

namespace TimeVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.SalesPerson}")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customerUsers = await _userManager.GetUsersInRoleAsync(RoleConstants.Customer);
            var totalCustomers = customerUsers.Count;

            var totalWatches = await _context.Watches.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0.00m;

            var lowStockCount = await _context.Watches
                .CountAsync(w => w.StockQuantity <= w.LowStockThreshold);

            var pendingOrdersCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending);

            // Recent Orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new RecentOrderAdminViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.ShippingFullName,
                    CustomerEmail = o.User != null && o.User.Email != null ? o.User.Email : string.Empty,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    ItemsCount = o.OrderItems.Sum(oi => oi.Quantity),
                    Status = o.Status.ToString(),
                    PaymentMethod = o.PaymentMethod.ToString()
                })
                .ToListAsync();

            // Low Stock Watches
            var lowStockWatches = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Images)
                .Where(w => w.StockQuantity <= w.LowStockThreshold)
                .OrderBy(w => w.StockQuantity)
                .Take(5)
                .Select(w => new LowStockWatchAdminViewModel
                {
                    Id = w.Id,
                    Name = w.Name,
                    SKU = w.SKU,
                    BrandName = w.Brand != null ? w.Brand.Name : "Prestige",
                    Price = w.Price,
                    StockQuantity = w.StockQuantity,
                    LowStockThreshold = w.LowStockThreshold,
                    ImageUrl = w.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null
                        ? w.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImagePath
                        : "/images/watches/placeholder.svg"
                })
                .ToListAsync();

            // Top Selling Watches
            var topSellingGroup = await _context.OrderItems
                .GroupBy(oi => oi.WatchId)
                .Select(g => new
                {
                    WatchId = g.Key,
                    UnitsSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(5)
                .ToListAsync();

            var topSellingWatches = new List<TopSellingWatchAdminViewModel>();
            foreach (var group in topSellingGroup)
            {
                var watch = await _context.Watches
                    .Include(w => w.Brand)
                    .Include(w => w.Images)
                    .FirstOrDefaultAsync(w => w.Id == group.WatchId);

                if (watch != null)
                {
                    topSellingWatches.Add(new TopSellingWatchAdminViewModel
                    {
                        Id = watch.Id,
                        Name = watch.Name,
                        SKU = watch.SKU,
                        BrandName = watch.Brand?.Name ?? "Prestige",
                        Price = watch.Price,
                        UnitsSold = group.UnitsSold,
                        TotalRevenueGenerated = group.Revenue,
                        ImageUrl = watch.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg"
                    });
                }
            }

            var model = new AdminDashboardViewModel
            {
                TotalWatches = totalWatches,
                TotalCustomers = totalCustomers,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                LowStockCount = lowStockCount,
                PendingOrdersCount = pendingOrdersCount,
                RecentOrders = recentOrders,
                LowStockWatches = lowStockWatches,
                TopSellingWatches = topSellingWatches
            };

            return View(model);
        }
    }
}
