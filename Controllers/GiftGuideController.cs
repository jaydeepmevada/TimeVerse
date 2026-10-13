using Microsoft.AspNetCore.Mvc;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.GiftGuide;

namespace TimeVerse.Controllers
{
    public class GiftGuideController : Controller
    {
        private readonly IWatchCatalogService _catalogService;

        public GiftGuideController(IWatchCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var menResult = await _catalogService.GetWatchesAsync(null, null, null, null, WatchGender.Men, null, null, "inStock", "featured", 1, 4);
            var womenResult = await _catalogService.GetWatchesAsync(null, null, null, null, WatchGender.Women, null, null, "inStock", "featured", 1, 4);
            var heirloomResult = await _catalogService.GetWatchesAsync(null, null, "Digital Watches", null, null, null, null, "inStock", "pricedesc", 1, 4);
            var under50kResult = await _catalogService.GetWatchesAsync(null, null, null, null, null, null, 50000, "inStock", "bestseller", 1, 4);

            var model = new GiftGuideViewModel
            {
                GiftsForHim = menResult.Watches,
                GiftsForHer = womenResult.Watches,
                HeirloomInvestments = heirloomResult.Watches,
                UnderFiftyThousand = under50kResult.Watches
            };

            return View(model);
        }
    }
}
