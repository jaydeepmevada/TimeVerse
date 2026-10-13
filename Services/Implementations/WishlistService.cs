using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Wishlist;

namespace TimeVerse.Services.Implementations
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WishlistViewModel> GetWishlistAsync(string userId)
        {
            var items = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Watch)
                    .ThenInclude(watch => watch!.Brand)
                .Include(w => w.Watch)
                    .ThenInclude(watch => watch!.Images)
                .ToListAsync();

            return new WishlistViewModel
            {
                Items = items.Where(i => i.Watch != null).Select(i => new WishlistItemViewModel
                {
                    Id = i.Id,
                    WatchId = i.WatchId,
                    WatchName = i.Watch!.Name,
                    BrandName = i.Watch.Brand?.Name ?? "Prestige Brand",
                    SKU = i.Watch.SKU,
                    Price = i.Watch.Price,
                    OriginalPrice = i.Watch.OriginalPrice,
                    ImageUrl = i.Watch.Images.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                    StockQuantity = i.Watch.StockQuantity,
                    AddedDate = i.CreatedAt
                }).ToList()
            };
        }

        public async Task<(bool Added, string Message)> ToggleWishlistAsync(string userId, int watchId)
        {
            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WatchId == watchId);

            var watch = await _context.Watches.FindAsync(watchId);
            var watchName = watch?.Name ?? "Timepiece";

            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                await _context.SaveChangesAsync();
                return (false, $"Removed '{watchName}' from your wishlist.");
            }

            var item = new WishlistItem
            {
                UserId = userId,
                WatchId = watchId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();
            return (true, $"Added '{watchName}' to your wishlist.");
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, int watchId)
        {
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WatchId == watchId);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            return await _context.WishlistItems.CountAsync(w => w.UserId == userId);
        }
    }
}
