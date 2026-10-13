using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;

namespace TimeVerse.Controllers
{
    /// <summary>
    /// Preserved as an alias to ShopController to maintain backward compatibility with /watches URLs.
    /// </summary>
    public class WatchesController : ShopController
    {
        public WatchesController(
            IWatchCatalogService catalogService,
            IReviewService reviewService,
            UserManager<ApplicationUser> userManager,
            ILogger<ShopController> logger)
            : base(catalogService, reviewService, userManager, logger)
        {
        }
    }
}
