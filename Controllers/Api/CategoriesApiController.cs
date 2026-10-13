using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.DTOs;
using TimeVerse.Models;

namespace TimeVerse.Controllers.Api
{
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves list of all active taxonomy categories.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Watches)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    WatchCount = c.Watches.Count(w => w.IsActive)
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Retrieves a specific category taxonomy by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
        {
            var cat = await _context.Categories
                .Include(c => c.Watches)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null) return NotFound(new { message = $"Category #{id} not found." });

            return Ok(new CategoryDto
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                IsActive = cat.IsActive,
                WatchCount = cat.Watches.Count(w => w.IsActive)
            });
        }

        /// <summary>
        /// Registers a new taxonomy category (Admin only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = RoleConstants.Admin)]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _context.Categories.AnyAsync(c => c.Name == dto.Name.Trim());
            if (exists) return BadRequest(new { message = $"Category '{dto.Name}' already exists." });

            var cat = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoryById), new { id = cat.Id }, new CategoryDto
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                IsActive = cat.IsActive,
                WatchCount = 0
            });
        }
    }
}
