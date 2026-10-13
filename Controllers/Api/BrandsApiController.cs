using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.DTOs;
using TimeVerse.Models;

namespace TimeVerse.Controllers.Api
{
    [ApiController]
    [Route("api/brands")]
    [Produces("application/json")]
    public class BrandsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BrandsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves list of all active luxury watch brands.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<BrandDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<BrandDto>>> GetBrands()
        {
            var brands = await _context.Brands
                .Include(b => b.Watches)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    IsActive = b.IsActive,
                    WatchCount = b.Watches.Count(w => w.IsActive)
                })
                .ToListAsync();

            return Ok(brands);
        }

        /// <summary>
        /// Retrieves a specific brand house by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BrandDto>> GetBrandById(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.Watches)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null) return NotFound(new { message = $"Brand #{id} not found." });

            return Ok(new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description,
                IsActive = brand.IsActive,
                WatchCount = brand.Watches.Count(w => w.IsActive)
            });
        }

        /// <summary>
        /// Registers a new brand house (Admin only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] BrandCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _context.Brands.AnyAsync(b => b.Name == dto.Name.Trim());
            if (exists) return BadRequest(new { message = $"Brand '{dto.Name}' already registered." });

            var brand = new Brand
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description,
                IsActive = brand.IsActive,
                WatchCount = 0
            });
        }
    }
}
