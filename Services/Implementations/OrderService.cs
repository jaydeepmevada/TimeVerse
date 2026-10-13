using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Checkout;
using TimeVerse.ViewModels.Orders;

namespace TimeVerse.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ApplicationDbContext context,
            ICartService cartService,
            ILogger<OrderService> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, string? OrderNumber)> CreateOrderAsync(
            string userId,
            CheckoutViewModel model,
            string? sessionId)
        {
            // 1. Fetch current cart items
            var cart = await _cartService.GetCartAsync(userId, sessionId);
            if (!cart.Items.Any())
            {
                return (false, "Your shopping bag is empty. Please add items before placing an order.", null);
            }

            // 2. CRITICAL TRANSACTION SAFETY
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify and Lock Stock for each item
                foreach (var item in cart.Items)
                {
                    var watch = await _context.Watches.FindAsync(item.WatchId);
                    if (watch == null || !watch.IsActive)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Watch model '{item.WatchName}' is no longer active.", null);
                    }

                    if (watch.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Insufficient vault inventory for '{watch.Name}'. Available: {watch.StockQuantity}, Requested: {item.Quantity}.", null);
                    }

                    // Deduct stock quantity atomically
                    watch.StockQuantity -= item.Quantity;
                }

                // Generate Unique Order Reference: TV-YYYYMMDD-XXXX
                var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
                var randomSuffix = Random.Shared.Next(1000, 9999);
                var orderNumber = $"TV-{todayStr}-{randomSuffix}";

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = cart.Subtotal,
                    ShippingFee = cart.ShippingFee,
                    DiscountAmount = 0,
                    FinalAmount = cart.GrandTotal,
                    Status = OrderStatus.Pending,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = model.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Pending : PaymentStatus.Completed,
                    ShippingFullName = model.ShippingFullName.Trim(),
                    ShippingAddressLine1 = model.ShippingAddressLine1.Trim(),
                    ShippingAddressLine2 = model.ShippingAddressLine2?.Trim(),
                    ShippingCity = model.ShippingCity.Trim(),
                    ShippingState = model.ShippingState.Trim(),
                    ShippingPostalCode = model.ShippingPostalCode.Trim(),
                    ShippingCountry = model.ShippingCountry.Trim(),
                    ShippingPhone = model.ShippingPhone.Trim(),
                    Notes = model.Notes?.Trim(),
                    TrackingNumber = $"ARM-TV-{todayStr}-{Random.Shared.Next(10000, 99999)}-IN",
                    CreatedAt = DateTime.UtcNow,
                    OrderItems = cart.Items.Select(ci => new OrderItem
                    {
                        WatchId = ci.WatchId,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.UnitPrice,
                        TotalPrice = ci.TotalPrice
                    }).ToList()
                };

                _context.Orders.Add(order);

                // If patron opted to save new address to address book
                if (model.SaveToAddressBook)
                {
                    var newAddress = new Address
                    {
                        ApplicationUserId = userId,
                        FullName = model.ShippingFullName.Trim(),
                        AddressLine1 = model.ShippingAddressLine1.Trim(),
                        AddressLine2 = model.ShippingAddressLine2?.Trim(),
                        City = model.ShippingCity.Trim(),
                        State = model.ShippingState.Trim(),
                        Country = model.ShippingCountry.Trim(),
                        PostalCode = model.ShippingPostalCode.Trim(),
                        Phone = model.ShippingPhone.Trim(),
                        IsDefault = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Addresses.Add(newAddress);
                }

                // Clear shopping cart
                await _cartService.ClearCartAsync(userId, sessionId);

                // Commit atomic transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderNumber} successfully placed for user {UserId} with total ₹{Amount}.", 
                    orderNumber, userId, order.FinalAmount);

                return (true, "Order placed successfully.", orderNumber);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed while creating order for user {UserId}.", userId);
                return (false, "A transactional error occurred during order fulfillment. Please try again.", null);
            }
        }

        public async Task<List<OrderSummaryViewModel>> GetCustomerOrdersAsync(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Include(o => o.OrderItems)
                .ToListAsync();

            return orders.Select(o => new OrderSummaryViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                FinalAmount = o.FinalAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                ItemCount = o.OrderItems.Sum(oi => oi.Quantity)
            }).ToList();
        }

        public async Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);
            return MapToDetails(order);
        }

        public async Task<OrderDetailsViewModel?> GetOrderByNumberAsync(string orderNumber, string? userId = null)
        {
            var cleanNumber = (orderNumber ?? string.Empty).Trim().ToLower();
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == cleanNumber);
            return MapToDetails(order);
        }

        public async Task<List<OrderDetailsViewModel>> GetAllOrdersAsync(OrderStatus? status = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Watch)
                        .ThenInclude(w => w!.Images)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query.ToListAsync();
            return orders.Select(MapToDetails).Where(o => o != null).Select(o => o!).ToList();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? trackingNumber = null)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            // If order was cancelled, restore stock!
            if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                foreach (var item in order.OrderItems)
                {
                    var watch = await _context.Watches.FindAsync(item.WatchId);
                    if (watch != null)
                    {
                        watch.StockQuantity += item.Quantity;
                    }
                }
            }

            order.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(trackingNumber))
            {
                order.TrackingNumber = trackingNumber.Trim();
            }
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} status updated to {Status}.", orderId, newStatus);
            return true;
        }

        private OrderDetailsViewModel? MapToDetails(Order? order)
        {
            if (order == null) return null;

            return new OrderDetailsViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.FinalAmount,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                ShippingFullName = order.ShippingFullName,
                ShippingEmail = order.User?.Email ?? string.Empty,
                ShippingAddressLine1 = order.ShippingAddressLine1,
                ShippingAddressLine2 = order.ShippingAddressLine2,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                ShippingPhone = order.ShippingPhone,
                TrackingNumber = order.TrackingNumber,
                Notes = order.Notes,
                Items = order.OrderItems.Select(oi => new OrderLineItemViewModel
                {
                    Id = oi.Id,
                    WatchId = oi.WatchId,
                    WatchName = oi.Watch?.Name ?? "Luxury Timepiece",
                    SKU = oi.Watch?.SKU ?? "N/A",
                    BrandName = oi.Watch?.Brand?.Name ?? "Prestige Brand",
                    ImageUrl = oi.Watch?.Images.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
    }
}
