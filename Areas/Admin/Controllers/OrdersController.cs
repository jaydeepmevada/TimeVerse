using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Orders;

namespace TimeVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            IOrderService orderService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, OrderStatus? status, string? dateFilter)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Images)
                .AsQueryable();

            // Status Filter
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            // Search Filter (Order Number, Customer Name, Email, City)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderNumber.ToLower().Contains(s) ||
                    o.ShippingFullName.ToLower().Contains(s) ||
                    (o.User != null && o.User.Email != null && o.User.Email.ToLower().Contains(s)) ||
                    o.ShippingCity.ToLower().Contains(s));
            }

            // Date Filter
            var now = DateTime.UtcNow;
            if (dateFilter == "today")
            {
                var today = now.Date;
                query = query.Where(o => o.OrderDate >= today);
            }
            else if (dateFilter == "week")
            {
                var sevenDaysAgo = now.AddDays(-7);
                query = query.Where(o => o.OrderDate >= sevenDaysAgo);
            }
            else if (dateFilter == "month")
            {
                var thirtyDaysAgo = now.AddDays(-30);
                query = query.Where(o => o.OrderDate >= thirtyDaysAgo);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            // Status counts for filter pills
            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;
            ViewBag.DateFilter = dateFilter;
            ViewBag.TotalCount = await _context.Orders.CountAsync();
            ViewBag.PendingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            ViewBag.ConfirmedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed);
            ViewBag.ProcessingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
            ViewBag.ShippedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
            ViewBag.DeliveredCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            ViewBag.CancelledCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

            var viewModels = orders.Select(o => new OrderDetailsViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                TotalAmount = o.TotalAmount,
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                FinalAmount = o.FinalAmount,
                ShippingFullName = o.ShippingFullName,
                ShippingEmail = o.User?.Email ?? string.Empty,
                ShippingPhone = o.ShippingPhone,
                ShippingAddressLine1 = o.ShippingAddressLine1,
                ShippingAddressLine2 = o.ShippingAddressLine2,
                ShippingCity = o.ShippingCity,
                ShippingState = o.ShippingState,
                ShippingPostalCode = o.ShippingPostalCode,
                ShippingCountry = o.ShippingCountry,
                TrackingNumber = o.TrackingNumber,
                Notes = o.Notes,
                Items = o.OrderItems.Select(oi => new OrderLineItemViewModel
                {
                    Id = oi.Id,
                    WatchId = oi.WatchId,
                    WatchName = oi.Watch?.Name ?? "Luxury Timepiece",
                    SKU = oi.Watch?.SKU ?? string.Empty,
                    BrandName = oi.Watch?.Brand?.Name ?? "Prestige",
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    ImageUrl = oi.Watch?.Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg"
                }).ToList()
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus, string? trackingNumber)
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, newStatus, trackingNumber);
            if (success)
            {
                _logger.LogInformation("Admin updated Order #{Id} status to {Status}.", id, newStatus);
                TempData["SuccessMessage"] = $"Order #{id} status successfully transitioned to {newStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Could not update status for order #{id}. Please ensure order exists.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
