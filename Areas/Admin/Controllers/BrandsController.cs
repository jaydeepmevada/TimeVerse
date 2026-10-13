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
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BrandsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands
                .Include(b => b.Watches)
                .OrderBy(b => b.Name)
                .Select(b => new BrandCrudViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    IsActive = b.IsActive,
                    WatchCount = b.Watches.Count
                })
                .ToListAsync();

            return View(brands);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new BrandCrudViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandCrudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var exists = await _context.Brands.AnyAsync(b => b.Name == model.Name.Trim());
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Brand name already exists.");
                return View(model);
            }

            var brand = new Brand
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Brand '{brand.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            var model = new BrandCrudViewModel
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description,
                IsActive = brand.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandCrudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var brand = await _context.Brands.FindAsync(model.Id);
            if (brand == null) return NotFound();

            brand.Name = model.Name.Trim();
            brand.Description = model.Description?.Trim();
            brand.IsActive = model.IsActive;
            brand.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Brand '{brand.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                brand.IsActive = !brand.IsActive;
                brand.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Brand status toggled to {(brand.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            var watchCount = await _context.Watches.CountAsync(w => w.BrandId == id);
            if (watchCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete brand '{brand.Name}' because {watchCount} timepiece(s) belong to it. Reassign or delete those watches first, or deactivate this brand.";
                return RedirectToAction(nameof(Index));
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Brand '{brand.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
