using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Cart;

namespace TimeVerse.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(ApplicationDbContext context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CartViewModel> GetCartAsync(string? userId, string? sessionId)
        {
            var query = _context.CartItems
                .Include(c => c.Watch)
                    .ThenInclude(w => w!.Brand)
                .Include(c => c.Watch)
                    .ThenInclude(w => w!.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(c => c.SessionId == sessionId);
            }
            else
            {
                return new CartViewModel();
            }

            var items = await query.ToListAsync();

            var viewModel = new CartViewModel
            {
                Items = items.Where(i => i.Watch != null).Select(i => new CartItemViewModel
                {
                    Id = i.Id,
                    WatchId = i.WatchId,
                    WatchName = i.Watch!.Name,
                    BrandName = i.Watch.Brand?.Name ?? "Prestige Brand",
                    SKU = i.Watch.SKU,
                    ImageUrl = i.Watch.Images.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                    UnitPrice = i.UnitPrice > 0 ? i.UnitPrice : i.Watch.Price,
                    Quantity = i.Quantity,
                    AvailableStock = i.Watch.StockQuantity
                }).ToList()
            };

            return viewModel;
        }

        public async Task<(bool Success, string Message)> AddToCartAsync(string? userId, string? sessionId, int watchId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return (false, "Session identifier is missing.");
            }

            var watch = await _context.Watches.FirstOrDefaultAsync(w => w.Id == watchId && w.IsActive);
            if (watch == null)
            {
                return (false, "Requested timepiece could not be found in active catalog.");
            }

            if (watch.StockQuantity <= 0)
            {
                return (false, $"'{watch.Name}' is currently out of stock.");
            }

            CartItem? existingItem = null;
            if (!string.IsNullOrEmpty(userId))
            {
                existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.WatchId == watchId);
            }
            else
            {
                existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.WatchId == watchId);
            }

            var newTotalQty = (existingItem?.Quantity ?? 0) + quantity;
            if (newTotalQty > watch.StockQuantity)
            {
                return (false, $"Cannot add {quantity} item(s). Only {watch.StockQuantity} available in vault.");
            }

            if (existingItem != null)
            {
                existingItem.Quantity = newTotalQty;
                existingItem.UnitPrice = watch.Price;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    SessionId = sessionId,
                    WatchId = watchId,
                    Quantity = quantity,
                    UnitPrice = watch.Price,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Added watch {WatchId} to cart for user {UserId}/session {SessionId}.", watchId, userId, sessionId);
            return (true, $"Added '{watch.Name}' to your shopping cart.");
        }

        public async Task<(bool Success, string Message)> UpdateQuantityAsync(string? userId, string? sessionId, int watchId, int newQuantity)
        {
            if (newQuantity <= 0)
            {
                await RemoveFromCartAsync(userId, sessionId, watchId);
                return (true, "Item removed from cart.");
            }

            var watch = await _context.Watches.FindAsync(watchId);
            if (watch == null)
            {
                return (false, "Timepiece reference not found.");
            }

            if (newQuantity > watch.StockQuantity)
            {
                return (false, $"Requested quantity exceeds available stock ({watch.StockQuantity} left).");
            }

            CartItem? item = null;
            if (!string.IsNullOrEmpty(userId))
            {
                item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.WatchId == watchId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                item = await _context.CartItems.FirstOrDefaultAsync(c => c.SessionId == sessionId && c.WatchId == watchId);
            }

            if (item == null)
            {
                return (false, "Cart item not found.");
            }

            item.Quantity = newQuantity;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, "Cart quantity updated.");
        }

        public async Task<bool> RemoveFromCartAsync(string? userId, string? sessionId, int watchId)
        {
            CartItem? item = null;
            if (!string.IsNullOrEmpty(userId))
            {
                item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.WatchId == watchId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                item = await _context.CartItems.FirstOrDefaultAsync(c => c.SessionId == sessionId && c.WatchId == watchId);
            }

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task ClearCartAsync(string? userId, string? sessionId)
        {
            IQueryable<CartItem> query = _context.CartItems;
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(c => c.SessionId == sessionId);
            }
            else
            {
                return;
            }

            var items = await query.ToListAsync();
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCartCountAsync(string? userId, string? sessionId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                return await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            }
            if (!string.IsNullOrEmpty(sessionId))
            {
                return await _context.CartItems.Where(c => c.SessionId == sessionId).SumAsync(c => c.Quantity);
            }
            return 0;
        }
    }
}
