using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.ViewModels.Sales;

namespace TimeVerse.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = $"{RoleConstants.SalesPerson},{RoleConstants.Admin}")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? stockFilter)
        {
            var query = _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(w => w.Name.ToLower().Contains(s) || w.SKU.ToLower().Contains(s) || (w.Brand != null && w.Brand.Name.ToLower().Contains(s)));
            }

            if (stockFilter == "lowStock")
            {
                query = query.Where(w => w.StockQuantity <= w.LowStockThreshold && w.StockQuantity > 0);
            }
            else if (stockFilter == "outOfStock")
            {
                query = query.Where(w => w.StockQuantity == 0);
            }
            else if (stockFilter == "inStock")
            {
                query = query.Where(w => w.StockQuantity > 0);
            }

            var watches = await query.OrderBy(w => w.StockQuantity).ToListAsync();

            var totalWatches = await _context.Watches.CountAsync();
            var lowStockCount = await _context.Watches.CountAsync(w => w.StockQuantity <= w.LowStockThreshold && w.StockQuantity > 0);
            var outOfStockCount = await _context.Watches.CountAsync(w => w.StockQuantity == 0);

            var model = new SalesStockListViewModel
            {
                Search = search,
                StockFilter = stockFilter,
                TotalWatchesCount = totalWatches,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                Watches = watches.Select(w => new SalesStockItemViewModel
                {
                    Id = w.Id,
                    Name = w.Name,
                    SKU = w.SKU,
                    BrandName = w.Brand?.Name ?? "Prestige Brand",
                    CategoryName = w.Category?.Name ?? "Luxury Watches",
                    MovementType = w.MovementType.ToString(),
                    Price = w.Price,
                    StockQuantity = w.StockQuantity,
                    LowStockThreshold = w.LowStockThreshold,
                    IsActive = w.IsActive,
                    ImageUrl = w.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg"
                }).ToList()
            };

            return View(model);
        }
    }
}
