using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels;

namespace TimeVerse.Services.Implementations
{
    public class WatchCatalogService : IWatchCatalogService
    {
        private readonly ApplicationDbContext _context;

        public WatchCatalogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HomeViewModel> GetHomeDataAsync()
        {
            var activeWatches = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .Where(w => w.IsActive)
                .ToListAsync();

            var featured = activeWatches.Where(w => w.IsFeatured).Take(6).ToList();
            if (featured.Count < 4)
            {
                featured = activeWatches.Take(6).ToList();
            }

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            var categoryImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Digital Watches", "/images/categories/cat-digital.jpg" },
                { "Digital", "/images/categories/cat-digital.jpg" },
                { "Smart Watches", "/images/categories/cat-smart.jpg" },
                { "Smart", "/images/categories/cat-smart.jpg" },
                { "Chronograph Watches", "/images/categories/cat-chrono.jpg" },
                { "Chronograph", "/images/categories/cat-chrono.jpg" },
                { "Chronographs", "/images/categories/cat-chrono.jpg" },
                { "Men's Watches", "/images/categories/cat-men.jpg" },
                { "Men's", "/images/categories/cat-men.jpg" },
                { "Mens", "/images/categories/cat-men.jpg" },
                { "Women's Watches", "/images/categories/cat-women.jpg" },
                { "Women's", "/images/categories/cat-women.jpg" },
                { "Womens", "/images/categories/cat-women.jpg" }
            };

            var categoryDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Digital Watches", "Multi-functional digital displays with precision timekeeping & toughness" },
                { "Smart Watches", "Modern connected health, fitness & digital features" },
                { "Chronograph Watches", "Precision stopwatch complication for sporting milestones" },
                { "Men's Watches", "Bold masculine silhouettes & refined engineering" },
                { "Women's Watches", "Graceful elegance with diamond-cut dials & jewelry pieces" }
            };

            var categoryOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Digital Watches", 1 },
                { "Digital", 1 },
                { "Smart Watches", 2 },
                { "Smart", 2 },
                { "Chronograph Watches", 3 },
                { "Chronograph", 3 },
                { "Chronographs", 3 },
                { "Men's Watches", 4 },
                { "Men's", 4 },
                { "Mens", 4 },
                { "Women's Watches", 5 },
                { "Women's", 5 },
                { "Womens", 5 }
            };

            var categorySpotlights = categories
                .OrderBy(cat => categoryOrder.TryGetValue(cat.Name, out var ord) ? ord : 99)
                .Select(cat => new CategorySpotlightItem
                {
                    Name = cat.Name,
                    Slug = cat.Name.ToLower().Replace(" ", "-").Replace("'", ""),
                    ImageUrl = categoryImages.TryGetValue(cat.Name, out var img) ? img : "/images/categories/cat-men.jpg",
                    Description = categoryDescriptions.TryGetValue(cat.Name, out var desc) ? desc : cat.Description ?? "",
                    ItemCount = activeWatches.Count(w => w.CategoryId == cat.Id)
                }).ToList();

            var brandNames = await _context.Brands
                .Where(b => b.IsActive)
                .Select(b => b.Name)
                .ToListAsync();

            if (!brandNames.Any())
            {
                brandNames = new List<string> { "Rolex", "Omega", "Seiko", "Casio", "Titan", "Fossil" };
            }

            var reviews = new List<CustomerReviewItem>
            {
                new()
                {
                    AuthorName = "Aarav Singhania",
                    WatchName = "TimeVerse Collection",
                    Rating = 5,
                    DateAgo = "2 days ago",
                    Comment = "Exceptional shopping experience with TimeVerse. Pristine packaging and precision accuracy. Truly the universe of time.",
                    IsVerifiedPurchaser = true,
                    AvatarUrl = "/images/avatars/avatar-1.svg"
                },
                new()
                {
                    AuthorName = "Vikramaditya Roy",
                    WatchName = "TimeVerse Chronograph",
                    Rating = 5,
                    DateAgo = "1 week ago",
                    Comment = "Outstanding craftsmanship and prompt delivery. Secure packaging and very responsive support.",
                    IsVerifiedPurchaser = true,
                    AvatarUrl = "/images/avatars/avatar-2.svg"
                },
                new()
                {
                    AuthorName = "Pooja Deshmukh",
                    WatchName = "TimeVerse Elegance",
                    Rating = 5,
                    DateAgo = "3 weeks ago",
                    Comment = "Astonishingly elegant and comfortable. Fits effortlessly for formal meetings.",
                    IsVerifiedPurchaser = true,
                    AvatarUrl = "/images/avatars/avatar-3.svg"
                },
                new()
                {
                    AuthorName = "Rajesh Mehra",
                    WatchName = "TimeVerse Digital Edition",
                    Rating = 5,
                    DateAgo = "1 month ago",
                    Comment = "Top-tier reliability and smooth ordering process. Delivered right on schedule.",
                    IsVerifiedPurchaser = true,
                    AvatarUrl = "/images/avatars/avatar-4.svg"
                }
            };

            return new HomeViewModel
            {
                FeaturedWatches = featured.Select(MapToCard).ToList(),
                NewArrivals = activeWatches.Where(w => w.IsNewArrival).Take(4).Select(MapToCard).ToList(),
                BestSellers = activeWatches.Where(w => w.IsBestSeller).Take(4).Select(MapToCard).ToList(),
                FeaturedCollection = featured.Select(MapToCard).ToList(),
                LimitedEditions = activeWatches.Where(w => w.IsLimitedEdition).Take(4).Select(MapToCard).ToList(),
                Categories = categorySpotlights,
                Reviews = reviews,
                PopularBrands = brandNames
            };
        }

        public async Task<WatchListViewModel> GetWatchesAsync(
            string? search,
            string? brand,
            string? category,
            MovementType? movement,
            WatchGender? gender,
            decimal? minPrice,
            decimal? maxPrice,
            string? stockFilter,
            string sort,
            int page,
            int pageSize)
        {
            var query = _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .Where(w => w.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(w =>
                    w.Name.ToLower().Contains(s) ||
                    w.SKU.ToLower().Contains(s) ||
                    (w.Brand != null && w.Brand.Name.ToLower().Contains(s)) ||
                    (w.Category != null && w.Category.Name.ToLower().Contains(s)) ||
                    w.Description.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(w => w.Brand != null && w.Brand.Name.ToLower() == brand.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catTrimmed = category.Trim().ToLower();
                query = query.Where(w => w.Category != null && (
                    w.Category.Name.ToLower() == catTrimmed ||
                    w.Category.Name.ToLower().Replace(" watches", "") == catTrimmed.Replace(" watches", "")
                ));
            }

            if (movement.HasValue)
            {
                query = query.Where(w => w.MovementType == movement.Value);
            }

            if (gender.HasValue)
            {
                query = query.Where(w => w.Gender == gender.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(w => w.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(w => w.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(stockFilter))
            {
                if (stockFilter.Equals("inStock", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(w => w.StockQuantity > 0);
                }
                else if (stockFilter.Equals("outOfStock", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(w => w.StockQuantity == 0);
                }
            }

            query = sort?.ToLower() switch
            {
                "newest" => query.OrderByDescending(w => w.CreatedAt).ThenByDescending(w => w.IsNewArrival),
                "priceasc" => query.OrderBy(w => w.Price),
                "pricedesc" => query.OrderByDescending(w => w.Price),
                "rating" => query.OrderByDescending(w => w.Rating),
                "bestseller" => query.OrderByDescending(w => w.IsBestSeller).ThenByDescending(w => w.ReviewCount),
                _ => query.OrderByDescending(w => w.IsFeatured).ThenByDescending(w => w.Rating)
            };

            var totalItems = await query.CountAsync();
            var validPage = page < 1 ? 1 : page;
            var validPageSize = pageSize < 1 ? 9 : pageSize;

            var items = await query
                .Skip((validPage - 1) * validPageSize)
                .Take(validPageSize)
                .ToListAsync();

            var allActiveWatches = _context.Watches.Where(w => w.IsActive);
            var minPriceAvail = await allActiveWatches.AnyAsync() ? await allActiveWatches.MinAsync(w => w.Price) : 0m;
            var maxPriceAvail = await allActiveWatches.AnyAsync() ? await allActiveWatches.MaxAsync(w => w.Price) : 100000m;

            var brandList = await _context.Brands.Where(b => b.IsActive).Select(b => b.Name).ToListAsync();
            var catList = await _context.Categories.Where(c => c.IsActive).Select(c => c.Name).ToListAsync();

            return new WatchListViewModel
            {
                Watches = items.Select(MapToCard).ToList(),
                Search = search,
                Brand = brand,
                Category = category,
                Movement = movement,
                Gender = gender,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                StockFilter = stockFilter,
                Sort = sort ?? "featured",
                CurrentPage = validPage,
                PageSize = validPageSize,
                TotalItems = totalItems,
                FilterOptions = new WatchFilterOptions
                {
                    Brands = brandList,
                    Categories = catList,
                    Movements = Enum.GetValues<MovementType>().ToList(),
                    Genders = Enum.GetValues<WatchGender>().ToList(),
                    MinPriceAvailable = minPriceAvail,
                    MaxPriceAvailable = maxPriceAvail
                }
            };
        }

        public async Task<WatchDetailsViewModel?> GetWatchByIdAsync(int id)
        {
            var watch = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);

            if (watch == null)
            {
                return null;
            }

            var related = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .Where(w => w.Id != watch.Id && w.IsActive && (w.BrandId == watch.BrandId || w.CategoryId == watch.CategoryId))
                .Take(3)
                .ToListAsync();

            var customerReviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.WatchId == watch.Id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new CustomerReviewItem
                {
                    AuthorName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}".Trim() : "TimeVerse Connoisseur",
                    WatchName = watch.Name,
                    Rating = r.Rating,
                    DateAgo = (DateTime.UtcNow - r.CreatedAt).TotalDays < 1 ? "Today" : $"{(int)(DateTime.UtcNow - r.CreatedAt).TotalDays} days ago",
                    Comment = r.Comment,
                    IsVerifiedPurchaser = r.IsVerifiedPurchase
                }).ToListAsync();

            return new WatchDetailsViewModel
            {
                Id = watch.Id,
                Name = watch.Name,
                SKU = watch.SKU,
                BrandName = watch.Brand?.Name ?? "Prestige Brand",
                CategoryName = watch.Category?.Name ?? "Luxury Watches",
                Price = watch.Price,
                OriginalPrice = watch.OriginalPrice,
                DiscountPercentage = watch.DiscountPercentage,
                StockQuantity = watch.StockQuantity,
                Rating = watch.Rating,
                ReviewCount = watch.ReviewCount,
                Description = watch.Description,
                MovementType = watch.MovementType,
                Gender = watch.Gender,
                CaseSize = watch.CaseSize,
                CaseMaterial = watch.CaseMaterial,
                DialColor = watch.DialColor,
                StrapMaterial = watch.StrapMaterial,
                WaterResistance = watch.WaterResistance,
                Warranty = watch.Warranty,
                CountryOfOrigin = watch.CountryOfOrigin,
                IsFeatured = watch.IsFeatured,
                IsBestSeller = watch.IsBestSeller,
                IsNewArrival = watch.IsNewArrival,
                IsLimitedEdition = watch.IsLimitedEdition,
                Images = watch.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImagePath).ToList(),
                RelatedWatches = related.Select(MapToCard).ToList(),
                CustomerReviews = customerReviews
            };
        }

        public async Task<List<WatchCardViewModel>> GetRelatedWatchesAsync(int watchId, int count = 3)
        {
            var current = await _context.Watches.FindAsync(watchId);
            if (current == null) return new List<WatchCardViewModel>();

            var related = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .Where(w => w.Id != watchId && w.IsActive && (w.BrandId == current.BrandId || w.CategoryId == current.CategoryId))
                .Take(count)
                .ToListAsync();

            return related.Select(MapToCard).ToList();
        }

        public async Task<WatchFilterOptions> GetFilterOptionsAsync()
        {
            var allActive = _context.Watches.Where(w => w.IsActive);
            var minPrice = await allActive.AnyAsync() ? await allActive.MinAsync(w => w.Price) : 0m;
            var maxPrice = await allActive.AnyAsync() ? await allActive.MaxAsync(w => w.Price) : 100000m;

            return new WatchFilterOptions
            {
                Brands = await _context.Brands.Where(b => b.IsActive).Select(b => b.Name).ToListAsync(),
                Categories = await _context.Categories.Where(c => c.IsActive).Select(c => c.Name).ToListAsync(),
                Movements = Enum.GetValues<MovementType>().ToList(),
                Genders = Enum.GetValues<WatchGender>().ToList(),
                MinPriceAvailable = minPrice,
                MaxPriceAvailable = maxPrice
            };
        }

        public Task<bool> SubmitInquiryAsync(ContactViewModel inquiry)
        {
            return Task.FromResult(true);
        }

        private static WatchCardViewModel MapToCard(Watch w)
        {
            return new WatchCardViewModel
            {
                Id = w.Id,
                Name = w.Name,
                SKU = w.SKU,
                BrandName = w.Brand?.Name ?? "Prestige Brand",
                CategoryName = w.Category?.Name ?? "Luxury Watches",
                Price = w.Price,
                OriginalPrice = w.OriginalPrice,
                DiscountPercentage = w.DiscountPercentage,
                Rating = w.Rating,
                ReviewCount = w.ReviewCount,
                ImageUrl = w.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                MovementType = w.MovementType,
                Gender = w.Gender,
                StockQuantity = w.StockQuantity,
                IsFeatured = w.IsFeatured,
                IsBestSeller = w.IsBestSeller,
                IsNewArrival = w.IsNewArrival,
                IsLimitedEdition = w.IsLimitedEdition
            };
        }
    }
}
