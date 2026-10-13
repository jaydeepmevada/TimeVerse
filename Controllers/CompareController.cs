using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Compare;

namespace TimeVerse.Controllers
{
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWatchCatalogService _catalogService;

        public CompareController(ApplicationDbContext context, IWatchCatalogService catalogService)
        {
            _context = context;
            _catalogService = catalogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? ids)
        {
            var idList = new List<int>();
            if (!string.IsNullOrEmpty(ids))
            {
                idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var parsed) ? parsed : 0)
                    .Where(i => i > 0)
                    .Take(4)
                    .ToList();
            }

            // Default to first 3 watches if no IDs provided
            if (!idList.Any())
            {
                idList = new List<int> { 1, 2, 3 };
            }

            var watches = new List<CompareItemViewModel>();
            foreach (var id in idList)
            {
                var details = await _catalogService.GetWatchByIdAsync(id);
                if (details != null)
                {
                    watches.Add(new CompareItemViewModel
                    {
                        Id = details.Id,
                        Name = details.Name,
                        BrandName = details.BrandName,
                        ImageUrl = details.PrimaryImageUrl,
                        Price = details.Price,
                        MovementType = details.MovementType,
                        CaseSize = details.CaseSize,
                        CaseMaterial = details.CaseMaterial,
                        DialColor = details.DialColor,
                        StrapMaterial = details.StrapMaterial,
                        WaterResistance = details.WaterResistance,
                        Warranty = details.Warranty,
                        CountryOfOrigin = details.CountryOfOrigin,
                        Rating = details.Rating,
                        InStock = details.InStock
                    });
                }
            }

            return View(new CompareViewModel { Watches = watches });
        }
    }
}
