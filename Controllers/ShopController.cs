using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;

namespace TimeVerse.Controllers
{
    public class ShopController : Controller
    {
        protected readonly IWatchCatalogService _catalogService;
        protected readonly IReviewService _reviewService;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ILogger<ShopController> _logger;

        public ShopController(
            IWatchCatalogService catalogService,
            IReviewService reviewService,
            UserManager<ApplicationUser> userManager,
            ILogger<ShopController> logger)
        {
            _catalogService = catalogService;
            _reviewService = reviewService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(
            string? search,
            string? brand,
            string? category,
            MovementType? movement,
            WatchGender? gender,
            decimal? minPrice,
            decimal? maxPrice,
            string? stockFilter,
            string sort = "featured",
            int page = 1,
            int pageSize = 9)
        {
            try
            {
                var model = await _catalogService.GetWatchesAsync(
                    search, brand, category, movement, gender, minPrice, maxPrice, stockFilter, sort, page, pageSize);

                return View("~/Views/Shop/Index.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving watch catalog.");
                return View("Error");
            }
        }

        [HttpGet]
        public virtual async Task<IActionResult> Details(int id)
        {
            try
            {
                var watch = await _catalogService.GetWatchByIdAsync(id);
                if (watch == null)
                {
                    _logger.LogWarning("Requested watch with ID {WatchId} was not found.", id);
                    return NotFound();
                }

                // Check for live database customer reviews
                var dbReviews = await _reviewService.GetWatchReviewsAsync(id);
                if (dbReviews != null && dbReviews.Any())
                {
                    watch.CustomerReviews = dbReviews;
                }

                // Check if current user can submit a review
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        watch.CanUserReview = await _reviewService.CanUserReviewWatchAsync(id, user.Id);
                    }
                }

                return View("~/Views/Shop/Details.cshtml", watch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watch details for ID {WatchId}.", id);
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> AddReview(int watchId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var (success, message) = await _reviewService.AddReviewAsync(watchId, user.Id, rating, comment);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Details), new { id = watchId });
        }
    }
}
