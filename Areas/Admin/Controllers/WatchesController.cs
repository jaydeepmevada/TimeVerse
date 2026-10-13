using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Admin;

namespace TimeVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.SalesPerson}")]
    public class WatchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWatchCatalogService _catalogService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<WatchesController> _logger;

        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private readonly string[] _permittedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;
        private const int MaxImagesCount = 8;

        public WatchesController(
            ApplicationDbContext context,
            IWatchCatalogService catalogService,
            IWebHostEnvironment environment,
            ILogger<WatchesController> logger)
        {
            _context = context;
            _catalogService = catalogService;
            _environment = environment;
            _logger = logger;
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
                query = query.Where(w => w.Name.ToLower().Contains(s) || w.SKU.ToLower().Contains(s));
            }

            if (stockFilter == "lowStock")
            {
                query = query.Where(w => w.StockQuantity <= w.LowStockThreshold);
            }
            else if (stockFilter == "outOfStock")
            {
                query = query.Where(w => w.StockQuantity == 0);
            }

            var watches = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();

            var model = new WatchListAdminViewModel
            {
                Search = search,
                StockFilter = stockFilter,
                Watches = watches.Select(w => new WatchAdminItemViewModel
                {
                    Id = w.Id,
                    Name = w.Name,
                    SKU = w.SKU,
                    BrandName = w.Brand?.Name ?? "Prestige Brand",
                    CategoryName = w.Category?.Name ?? "Luxury Watches",
                    Price = w.Price,
                    StockQuantity = w.StockQuantity,
                    ImageUrl = w.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                    IsActive = w.IsActive,
                    IsFeatured = w.IsFeatured,
                    IsLowStock = w.StockQuantity <= w.LowStockThreshold
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateWatchViewModel();
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWatchViewModel model)
        {
            await PopulateDropdownsAsync(model);

            // Auto-generate unique SKU if not provided
            if (string.IsNullOrWhiteSpace(model.SKU))
            {
                var brand = await _context.Brands.FindAsync(model.BrandId);
                var prefix = brand != null && !string.IsNullOrWhiteSpace(brand.Name)
                    ? new string(brand.Name.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpper()
                    : "TV";
                if (string.IsNullOrWhiteSpace(prefix)) prefix = "TV";
                model.SKU = $"TV-{prefix}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            }
            else
            {
                var existingSku = await _context.Watches.AnyAsync(w => w.SKU == model.SKU.Trim());
                if (existingSku)
                {
                    ModelState.AddModelError(nameof(model.SKU), "A timepiece with this SKU already exists.");
                    return View(model);
                }
            }

            // Auto-calculate discount percentage based on Price (MRP) and Selling Price
            if (model.OriginalPrice.HasValue && model.OriginalPrice.Value > 0 && model.OriginalPrice.Value > model.Price)
            {
                model.DiscountPercentage = Math.Round(((decimal)(model.OriginalPrice.Value - model.Price) / model.OriginalPrice.Value) * 100, 1);
            }
            else
            {
                model.DiscountPercentage = 0;
            }

            // Set appropriate gender based on category
            var selectedCategory = await _context.Categories.FindAsync(model.CategoryId);
            if (selectedCategory != null)
            {
                if (selectedCategory.Name.Contains("Women", StringComparison.OrdinalIgnoreCase))
                    model.Gender = WatchGender.Women;
                else if (selectedCategory.Name.Contains("Men", StringComparison.OrdinalIgnoreCase))
                    model.Gender = WatchGender.Men;
                else
                    model.Gender = WatchGender.Unisex;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var watch = new Watch
            {
                Name = model.Name.Trim(),
                SKU = model.SKU.Trim().ToUpper(),
                Description = model.Description.Trim(),
                BrandId = model.BrandId,
                CategoryId = model.CategoryId,
                Price = model.Price,
                OriginalPrice = model.OriginalPrice,
                DiscountPercentage = model.DiscountPercentage,
                StockQuantity = model.StockQuantity,
                LowStockThreshold = model.LowStockThreshold,
                MovementType = model.MovementType,
                Gender = model.Gender,
                CaseSize = model.CaseSize.Trim(),
                CaseMaterial = model.CaseMaterial.Trim(),
                DialColor = model.DialColor.Trim(),
                StrapMaterial = model.StrapMaterial.Trim(),
                WaterResistance = model.WaterResistance.Trim(),
                Warranty = model.Warranty.Trim(),
                CountryOfOrigin = model.CountryOfOrigin.Trim(),
                IsFeatured = model.IsFeatured,
                IsBestSeller = model.IsBestSeller,
                IsNewArrival = model.IsNewArrival,
                IsLimitedEdition = model.IsLimitedEdition,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            // Multi-Image Upload & Secure GUID Storage
            if (model.Images != null && model.Images.Count > 0)
            {
                if (model.Images.Count > MaxImagesCount)
                {
                    ModelState.AddModelError(nameof(model.Images), $"You can upload a maximum of {MaxImagesCount} images.");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "watches");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                int order = 1;
                foreach (var file in model.Images)
                {
                    if (file.Length == 0) continue;
                    if (file.Length > MaxFileSizeBytes) continue;

                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_permittedExtensions.Contains(extension)) continue;

                    var safeFileName = $"{Guid.NewGuid()}{extension}";
                    var targetPath = Path.Combine(uploadsFolder, safeFileName);
                    using (var stream = new FileStream(targetPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    watch.Images.Add(new WatchImage
                    {
                        ImagePath = $"/uploads/watches/{safeFileName}",
                        IsPrimary = (order == 1),
                        DisplayOrder = order++
                    });
                }
            }

            if (!watch.Images.Any())
            {
                watch.Images.Add(new WatchImage
                {
                    ImagePath = "/images/watches/placeholder.svg",
                    IsPrimary = true,
                    DisplayOrder = 1
                });
            }

            _context.Watches.Add(watch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin created watch {Name} (ID {Id}).", watch.Name, watch.Id);
            TempData["SuccessMessage"] = $"Timepiece '{watch.Name}' registered successfully!";
            return RedirectToAction(nameof(Specifications), new { id = watch.Id, isNew = true });
        }

        [HttpGet]
        public async Task<IActionResult> Specifications(int id, bool isNew = false)
        {
            var watch = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (watch == null) return NotFound();

            var model = new WatchSpecificationsViewModel
            {
                WatchId = watch.Id,
                WatchName = watch.Name,
                SKU = watch.SKU,
                BrandName = watch.Brand?.Name ?? "Prestige Brand",
                CategoryName = watch.Category?.Name ?? "Watches",
                PrimaryImageUrl = watch.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                IsNew = isNew,
                MovementType = watch.MovementType,
                Gender = watch.Gender,
                CaseSize = string.IsNullOrWhiteSpace(watch.CaseSize) ? "40mm" : watch.CaseSize,
                CaseMaterial = string.IsNullOrWhiteSpace(watch.CaseMaterial) ? "Stainless Steel" : watch.CaseMaterial,
                DialColor = string.IsNullOrWhiteSpace(watch.DialColor) ? "Black" : watch.DialColor,
                StrapMaterial = string.IsNullOrWhiteSpace(watch.StrapMaterial) ? "Stainless Steel" : watch.StrapMaterial,
                WaterResistance = string.IsNullOrWhiteSpace(watch.WaterResistance) ? "100m / 10 ATM" : watch.WaterResistance,
                Warranty = string.IsNullOrWhiteSpace(watch.Warranty) ? "2 Years International" : watch.Warranty,
                CountryOfOrigin = string.IsNullOrWhiteSpace(watch.CountryOfOrigin) ? "Switzerland" : watch.CountryOfOrigin,
                IsFeatured = watch.IsFeatured,
                IsBestSeller = watch.IsBestSeller,
                IsNewArrival = watch.IsNewArrival,
                IsLimitedEdition = watch.IsLimitedEdition
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Specifications(WatchSpecificationsViewModel model)
        {
            var watch = await _context.Watches.FindAsync(model.WatchId);
            if (watch == null) return NotFound();

            watch.MovementType = model.MovementType;
            watch.Gender = model.Gender;
            watch.CaseSize = string.IsNullOrWhiteSpace(model.CaseSize) ? "40mm" : model.CaseSize.Trim();
            watch.CaseMaterial = string.IsNullOrWhiteSpace(model.CaseMaterial) ? "Stainless Steel" : model.CaseMaterial.Trim();
            watch.DialColor = string.IsNullOrWhiteSpace(model.DialColor) ? "Black" : model.DialColor.Trim();
            watch.StrapMaterial = string.IsNullOrWhiteSpace(model.StrapMaterial) ? "Stainless Steel" : model.StrapMaterial.Trim();
            watch.WaterResistance = string.IsNullOrWhiteSpace(model.WaterResistance) ? "100m / 10 ATM" : model.WaterResistance.Trim();
            watch.Warranty = string.IsNullOrWhiteSpace(model.Warranty) ? "2 Years International" : model.Warranty.Trim();
            watch.CountryOfOrigin = string.IsNullOrWhiteSpace(model.CountryOfOrigin) ? "Switzerland" : model.CountryOfOrigin.Trim();
            watch.IsFeatured = model.IsFeatured;
            watch.IsBestSeller = model.IsBestSeller;
            watch.IsNewArrival = model.IsNewArrival;
            watch.IsLimitedEdition = model.IsLimitedEdition;
            watch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Specifications for '{watch.Name}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var watch = await _context.Watches
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (watch == null) return NotFound();

            var model = new EditWatchViewModel
            {
                Id = watch.Id,
                Name = watch.Name,
                SKU = watch.SKU,
                Description = watch.Description,
                BrandId = watch.BrandId,
                CategoryId = watch.CategoryId,
                Price = (int)Math.Round(watch.Price),
                OriginalPrice = watch.OriginalPrice.HasValue ? (int)Math.Round(watch.OriginalPrice.Value) : null,
                DiscountPercentage = watch.DiscountPercentage,
                StockQuantity = watch.StockQuantity,
                LowStockThreshold = watch.LowStockThreshold,
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
                IsActive = watch.IsActive,
                ExistingImages = watch.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImagePath).ToList()
            };

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditWatchViewModel model)
        {
            await PopulateDropdownsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var watch = await _context.Watches
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == model.Id);

            if (watch == null) return NotFound();

            watch.Name = model.Name.Trim();
            watch.SKU = model.SKU.Trim().ToUpper();
            watch.Description = model.Description.Trim();
            watch.BrandId = model.BrandId;
            watch.CategoryId = model.CategoryId;
            watch.Price = model.Price;
            watch.OriginalPrice = model.OriginalPrice;
            watch.DiscountPercentage = model.DiscountPercentage;
            watch.StockQuantity = model.StockQuantity;
            watch.LowStockThreshold = model.LowStockThreshold;
            watch.MovementType = model.MovementType;
            watch.Gender = model.Gender;
            watch.CaseSize = model.CaseSize.Trim();
            watch.CaseMaterial = model.CaseMaterial.Trim();
            watch.DialColor = model.DialColor.Trim();
            watch.StrapMaterial = model.StrapMaterial.Trim();
            watch.WaterResistance = model.WaterResistance.Trim();
            watch.Warranty = model.Warranty.Trim();
            watch.CountryOfOrigin = model.CountryOfOrigin.Trim();
            watch.IsFeatured = model.IsFeatured;
            watch.IsBestSeller = model.IsBestSeller;
            watch.IsNewArrival = model.IsNewArrival;
            watch.IsLimitedEdition = model.IsLimitedEdition;
            watch.IsActive = model.IsActive;
            watch.UpdatedAt = DateTime.UtcNow;

            // Handle optional new images upload
            if (model.Images != null && model.Images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "watches");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                int order = watch.Images.Count + 1;
                foreach (var file in model.Images)
                {
                    if (file.Length == 0 || file.Length > MaxFileSizeBytes) continue;
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!_permittedExtensions.Contains(extension)) continue;

                    var safeFileName = $"{Guid.NewGuid()}{extension}";
                    var targetPath = Path.Combine(uploadsFolder, safeFileName);
                    using (var stream = new FileStream(targetPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    watch.Images.Add(new WatchImage
                    {
                        ImagePath = $"/uploads/watches/{safeFileName}",
                        IsPrimary = false,
                        DisplayOrder = order++
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Timepiece '{watch.Name}' details updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            // SOFT DELETE PATTERN: We do not permanently delete, preserving historic order records!
            var watch = await _context.Watches.FindAsync(id);
            if (watch != null)
            {
                watch.IsActive = !watch.IsActive;
                watch.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = watch.IsActive 
                    ? $"Timepiece '{watch.Name}' reactivated in catalog." 
                    : $"Timepiece '{watch.Name}' soft-deleted (deactivated) from public store.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var watch = await _context.Watches
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (watch == null)
            {
                return NotFound();
            }

            // FOREIGN KEY AUDIT PROTECTION: Has this timepiece ever been ordered?
            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.WatchId == id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] = $"Timepiece '{watch.Name}' cannot be permanently deleted because it is referenced in past customer orders. Deactivate it instead to hide it from the storefront while protecting order audit records.";
                return RedirectToAction(nameof(Index));
            }

            _context.Watches.Remove(watch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin deleted timepiece {Name} (ID {Id}).", watch.Name, watch.Id);
            TempData["SuccessMessage"] = $"Timepiece '{watch.Name}' has been permanently removed from the catalog.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(CreateWatchViewModel model)
        {
            var brands = await _context.Brands.Where(b => b.IsActive).ToListAsync();
            var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

            model.BrandList = brands.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            }).ToList();

            model.CategoryList = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
        }
    }
}
