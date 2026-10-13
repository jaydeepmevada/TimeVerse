using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.DTOs;
using TimeVerse.Models;

namespace TimeVerse.Controllers.Api
{
    [ApiController]
    [Route("api/watches")]
    [Produces("application/json")]
    public class WatchesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WatchesApiController> _logger;

        public WatchesApiController(ApplicationDbContext context, ILogger<WatchesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves paginated list of luxury timepieces with multifaceted filtering and sorting.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(WatchListResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<WatchListResponseDto>> GetWatches(
            [FromQuery] string? search,
            [FromQuery] string? brand,
            [FromQuery] string? category,
            [FromQuery] MovementType? movement,
            [FromQuery] WatchGender? gender,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? stockFilter,
            [FromQuery] string? sort = "featured",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
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
                query = query.Where(w => w.Name.ToLower().Contains(s) || w.SKU.ToLower().Contains(s) || (w.Brand != null && w.Brand.Name.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(w => w.Brand != null && w.Brand.Name.ToLower() == brand.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(w => w.Category != null && w.Category.Name.ToLower() == category.Trim().ToLower());
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

            if (stockFilter == "inStock")
            {
                query = query.Where(w => w.StockQuantity > 0);
            }
            else if (stockFilter == "outOfStock")
            {
                query = query.Where(w => w.StockQuantity == 0);
            }

            query = sort?.ToLower() switch
            {
                "priceasc" => query.OrderBy(w => w.Price),
                "pricedesc" => query.OrderByDescending(w => w.Price),
                "rating" => query.OrderByDescending(w => w.Rating),
                "newest" => query.OrderByDescending(w => w.CreatedAt),
                "bestseller" => query.OrderByDescending(w => w.IsBestSeller),
                _ => query.OrderByDescending(w => w.IsFeatured).ThenByDescending(w => w.Rating)
            };

            var totalCount = await query.CountAsync();
            var validPage = page < 1 ? 1 : page;
            var validPageSize = pageSize < 1 ? 12 : pageSize;

            var watches = await query
                .Skip((validPage - 1) * validPageSize)
                .Take(validPageSize)
                .ToListAsync();

            var response = new WatchListResponseDto
            {
                Items = watches.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = validPage,
                PageSize = validPageSize
            };

            return Ok(response);
        }

        /// <summary>
        /// Retrieves full horological details for a specific timepiece by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WatchDto>> GetWatchById(int id)
        {
            var watch = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (watch == null)
            {
                return NotFound(new { message = $"Timepiece reference #{id} was not found." });
            }

            return Ok(MapToDto(watch));
        }

        /// <summary>
        /// Registers a new timepiece model in the vault catalog.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(WatchDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WatchDto>> CreateWatch([FromBody] WatchCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingSku = await _context.Watches.AnyAsync(w => w.SKU == dto.SKU.Trim());
            if (existingSku)
            {
                return BadRequest(new { message = $"A timepiece with SKU '{dto.SKU}' already exists." });
            }

            var watch = new Watch
            {
                Name = dto.Name.Trim(),
                SKU = dto.SKU.Trim().ToUpper(),
                BrandId = dto.BrandId,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                OriginalPrice = dto.OriginalPrice,
                DiscountPercentage = dto.DiscountPercentage,
                StockQuantity = dto.StockQuantity,
                MovementType = dto.MovementType,
                Gender = dto.Gender,
                CaseSize = dto.CaseSize,
                CaseMaterial = dto.CaseMaterial,
                DialColor = dto.DialColor,
                StrapMaterial = dto.StrapMaterial,
                WaterResistance = dto.WaterResistance,
                Warranty = dto.Warranty,
                CountryOfOrigin = dto.CountryOfOrigin,
                Description = dto.Description,
                IsFeatured = dto.IsFeatured,
                IsBestSeller = dto.IsBestSeller,
                IsNewArrival = dto.IsNewArrival,
                IsLimitedEdition = dto.IsLimitedEdition,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Watches.Add(watch);
            await _context.SaveChangesAsync();

            var loaded = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .FirstAsync(w => w.Id == watch.Id);

            return CreatedAtAction(nameof(GetWatchById), new { id = watch.Id }, MapToDto(loaded));
        }

        /// <summary>
        /// Updates an existing timepiece model in the catalog.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(WatchDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WatchDto>> UpdateWatch(int id, [FromBody] WatchUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var watch = await _context.Watches
                .Include(w => w.Brand)
                .Include(w => w.Category)
                .Include(w => w.Images)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (watch == null) return NotFound(new { message = $"Timepiece #{id} not found." });

            watch.Name = dto.Name.Trim();
            watch.SKU = dto.SKU.Trim().ToUpper();
            watch.BrandId = dto.BrandId;
            watch.CategoryId = dto.CategoryId;
            watch.Price = dto.Price;
            watch.OriginalPrice = dto.OriginalPrice;
            watch.DiscountPercentage = dto.DiscountPercentage;
            watch.StockQuantity = dto.StockQuantity;
            watch.MovementType = dto.MovementType;
            watch.Gender = dto.Gender;
            watch.CaseSize = dto.CaseSize;
            watch.CaseMaterial = dto.CaseMaterial;
            watch.DialColor = dto.DialColor;
            watch.StrapMaterial = dto.StrapMaterial;
            watch.WaterResistance = dto.WaterResistance;
            watch.Warranty = dto.Warranty;
            watch.CountryOfOrigin = dto.CountryOfOrigin;
            watch.Description = dto.Description;
            watch.IsFeatured = dto.IsFeatured;
            watch.IsBestSeller = dto.IsBestSeller;
            watch.IsNewArrival = dto.IsNewArrival;
            watch.IsLimitedEdition = dto.IsLimitedEdition;
            watch.IsActive = dto.IsActive;
            watch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDto(watch));
        }

        /// <summary>
        /// Soft-deletes (deactivates) a timepiece from the public catalog.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWatch(int id)
        {
            var watch = await _context.Watches.FindAsync(id);
            if (watch == null) return NotFound(new { message = $"Timepiece #{id} not found." });

            watch.IsActive = false;
            watch.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Timepiece '{watch.Name}' soft-deleted (deactivated) from public catalog.", id = watch.Id, isActive = false });
        }

        private static WatchDto MapToDto(Watch w)
        {
            return new WatchDto
            {
                Id = w.Id,
                Name = w.Name,
                SKU = w.SKU,
                BrandId = w.BrandId,
                BrandName = w.Brand?.Name ?? "Prestige Horology",
                CategoryId = w.CategoryId,
                CategoryName = w.Category?.Name ?? "Luxury Watches",
                Price = w.Price,
                OriginalPrice = w.OriginalPrice,
                DiscountPercentage = w.DiscountPercentage,
                StockQuantity = w.StockQuantity,
                MovementType = w.MovementType,
                Gender = w.Gender,
                CaseSize = w.CaseSize,
                CaseMaterial = w.CaseMaterial,
                DialColor = w.DialColor,
                StrapMaterial = w.StrapMaterial,
                WaterResistance = w.WaterResistance,
                Warranty = w.Warranty,
                CountryOfOrigin = w.CountryOfOrigin,
                Rating = w.Rating,
                ReviewCount = w.ReviewCount,
                IsFeatured = w.IsFeatured,
                IsBestSeller = w.IsBestSeller,
                IsNewArrival = w.IsNewArrival,
                IsLimitedEdition = w.IsLimitedEdition,
                IsActive = w.IsActive,
                Description = w.Description,
                Images = w.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImagePath).ToList()
            };
        }
    }
}
