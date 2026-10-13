using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Checkout;
using TimeVerse.ViewModels.Customer;

namespace TimeVerse.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ICartService cartService,
            IOrderService orderService,
            ApplicationDbContext context,
            ILogger<CheckoutController> logger)
        {
            _cartService = cartService;
            _orderService = orderService;
            _context = context;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private string SessionId => HttpContext.Session.GetString("TimeVerse_SessionId") ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your shopping bag is empty. Please add watches before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            var addresses = await _context.Addresses
                .Where(a => a.ApplicationUserId == CurrentUserId)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new AddressViewModel
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    State = a.State,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    Phone = a.Phone,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            var defaultAddr = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();

            var model = new CheckoutViewModel
            {
                Cart = cart,
                SavedAddresses = addresses,
                SelectedAddressId = defaultAddr?.Id,
                ShippingFullName = defaultAddr?.FullName ?? User.Identity?.Name ?? string.Empty,
                ShippingAddressLine1 = defaultAddr?.AddressLine1 ?? string.Empty,
                ShippingAddressLine2 = defaultAddr?.AddressLine2,
                ShippingCity = defaultAddr?.City ?? string.Empty,
                ShippingState = defaultAddr?.State ?? string.Empty,
                ShippingPostalCode = defaultAddr?.PostalCode ?? string.Empty,
                ShippingCountry = defaultAddr?.Country ?? "India",
                ShippingPhone = defaultAddr?.Phone ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            // If user selected a saved address, pull its details
            if (model.SelectedAddressId.HasValue && model.SelectedAddressId.Value > 0)
            {
                var savedAddr = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == model.SelectedAddressId.Value && a.ApplicationUserId == CurrentUserId);
                if (savedAddr != null)
                {
                    model.ShippingFullName = savedAddr.FullName;
                    model.ShippingAddressLine1 = savedAddr.AddressLine1;
                    model.ShippingAddressLine2 = savedAddr.AddressLine2;
                    model.ShippingCity = savedAddr.City;
                    model.ShippingState = savedAddr.State;
                    model.ShippingPostalCode = savedAddr.PostalCode;
                    model.ShippingCountry = savedAddr.Country;
                    model.ShippingPhone = savedAddr.Phone;
                }
            }

            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var err in entry.Value.Errors)
                    {
                        _logger.LogWarning("Checkout ModelState Error - {Key}: {Error}", entry.Key, err.ErrorMessage);
                    }
                }
                model.Cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
                return View("Index", model);
            }

            var (success, message, orderNumber) = await _orderService.CreateOrderAsync(CurrentUserId, model, SessionId);
            _logger.LogInformation("CreateOrderAsync result: Success={Success}, Message={Message}, OrderNumber={OrderNumber}", success, message, orderNumber);
            if (success && !string.IsNullOrEmpty(orderNumber))
            {
                return RedirectToAction(nameof(Confirmation), new { orderNumber });
            }

            TempData["ErrorMessage"] = message;
            model.Cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(string orderNumber)
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber, CurrentUserId);
            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderConfirmationViewModel
            {
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                FinalAmount = order.FinalAmount,
                ShippingFullName = order.ShippingFullName,
                FullAddress = $"{order.ShippingAddressLine1}, {(string.IsNullOrEmpty(order.ShippingAddressLine2) ? "" : order.ShippingAddressLine2 + ", ")}{order.ShippingCity}, {order.ShippingState} - {order.ShippingPostalCode}, {order.ShippingCountry}",
                Phone = order.ShippingPhone,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                Items = order.Items.Select(i => new ViewModels.Cart.CartItemViewModel
                {
                    WatchId = i.WatchId,
                    WatchName = i.WatchName,
                    SKU = i.SKU,
                    ImageUrl = i.ImageUrl,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            return View(model);
        }
    }
}
