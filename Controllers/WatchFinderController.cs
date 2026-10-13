using Microsoft.AspNetCore.Mvc;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.WatchFinder;

namespace TimeVerse.Controllers
{
    public class WatchFinderController : Controller
    {
        private readonly IWatchCatalogService _catalogService;

        public WatchFinderController(IWatchCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new WatchFinderWizardViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(WatchFinderWizardViewModel model)
        {
            decimal? minPrice = null;
            decimal? maxPrice = null;

            if (model.BudgetRange == "under30k")
            {
                maxPrice = 30000;
            }
            else if (model.BudgetRange == "30k-100k")
            {
                minPrice = 30000;
                maxPrice = 100000;
            }
            else if (model.BudgetRange == "above100k")
            {
                minPrice = 100000;
            }

            MovementType? movement = model.Lifestyle switch
            {
                "daily" => MovementType.Automatic,
                "racing" => MovementType.Chronograph,
                "smart" => MovementType.Smart,
                _ => null
            };

            var catalog = await _catalogService.GetWatchesAsync(
                search: null,
                brand: null,
                category: model.Lifestyle == "luxury" ? "Digital Watches" : null,
                movement: movement,
                gender: model.SelectedGender,
                minPrice: minPrice,
                maxPrice: maxPrice,
                stockFilter: "inStock",
                sort: "rating",
                page: 1,
                pageSize: 6);

            model.RecommendedWatches = catalog.Watches;
            model.HasSearched = true;
            return View(model);
        }
    }
}
