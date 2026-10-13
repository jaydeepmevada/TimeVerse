using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Orders;
using TimeVerse.ViewModels.Sales;

namespace TimeVerse.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = $"{RoleConstants.SalesPerson},{RoleConstants.Admin}")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;

        public DashboardController(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var sevenDaysAgo = now.AddDays(-7);

            var pendingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            var confirmedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed);
            var processingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
            var shippedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
            var deliveredCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);

            var todayOrders = await _context.Orders
                .Where(o => o.OrderDate >= today && o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            var todaySales = todayOrders.Sum(o => o.TotalAmount);
            var todayOrdersCount = todayOrders.Count;

            var weekSales = await _context.Orders
                .Where(o => o.OrderDate >= sevenDaysAgo && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0.00m;

            // Active Dispatch Queue: Pending, Confirmed, or Processing orders
            var activeOrdersRaw = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Processing)
                .OrderBy(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var activeQueue = activeOrdersRaw.Select(o => new OrderDetailsViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                TotalAmount = o.TotalAmount,
                FinalAmount = o.FinalAmount,
                ShippingFullName = o.ShippingFullName,
                ShippingEmail = o.User?.Email ?? string.Empty,
                ShippingPhone = o.ShippingPhone,
                ShippingCity = o.ShippingCity,
                TrackingNumber = o.TrackingNumber,
                Items = o.OrderItems.Select(oi => new OrderLineItemViewModel
                {
                    Id = oi.Id,
                    WatchName = oi.Watch?.Name ?? "Timepiece",
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity
                }).ToList()
            }).ToList();

            var model = new SalesDashboardViewModel
            {
                PendingOrdersCount = pendingCount,
                ConfirmedOrdersCount = confirmedCount,
                ProcessingOrdersCount = processingCount,
                ShippedOrdersCount = shippedCount,
                DeliveredOrdersCount = deliveredCount,
                TodayOrdersCount = todayOrdersCount,
                TodaySales = todaySales,
                WeekSales = weekSales,
                NewInquiriesCount = 0,
                ActiveOrdersQueue = activeQueue
            };

            return View(model);
        }
    }
}
