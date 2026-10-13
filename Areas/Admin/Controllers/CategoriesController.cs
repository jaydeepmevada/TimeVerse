using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.ViewModels.Admin;

namespace TimeVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.SalesPerson}")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Watches)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryCrudViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    WatchCount = c.Watches.Count
                })
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryCrudViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCrudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var exists = await _context.Categories.AnyAsync(c => c.Name == model.Name.Trim());
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Category name already exists.");
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' registered.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var model = new CategoryCrudViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryCrudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null) return NotFound();

            category.Name = model.Name.Trim();
            category.Description = model.Description?.Trim();
            category.IsActive = model.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Category '{category.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                category.IsActive = !category.IsActive;
                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category status toggled to {(category.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var watchCount = await _context.Watches.CountAsync(w => w.CategoryId == id);
            if (watchCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because {watchCount} timepiece(s) belong to it. Reassign or delete those watches first, or deactivate this category.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
