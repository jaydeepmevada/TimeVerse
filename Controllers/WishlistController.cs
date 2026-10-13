using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Wishlist;

namespace TimeVerse.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly ICartService _cartService;
        private readonly IWatchCatalogService _catalogService;

        public WishlistController(
            IWishlistService wishlistService,
            ICartService cartService,
            IWatchCatalogService catalogService)
        {
            _wishlistService = wishlistService;
            _cartService = cartService;
            _catalogService = catalogService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private string SessionId
        {
            get
            {
                var id = HttpContext.Session.GetString("TimeVerse_SessionId");
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString();
                    HttpContext.Session.SetString("TimeVerse_SessionId", id);
                }
                return id;
            }
        }

        private List<int> SessionWishlistIds
        {
            get
            {
                var val = HttpContext.Session.GetString("TimeVerse_Wishlist");
                if (string.IsNullOrEmpty(val)) return new List<int>();
                return val.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => int.TryParse(s, out var id) ? id : 0)
                          .Where(id => id > 0)
                          .Distinct()
                          .ToList();
            }
            set
            {
                HttpContext.Session.SetString("TimeVerse_Wishlist", string.Join(",", value));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            int count = 0;
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(CurrentUserId))
            {
                count = await _wishlistService.GetWishlistCountAsync(CurrentUserId);
            }
            else
            {
                count = SessionWishlistIds.Count;
            }
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(CurrentUserId))
            {
                var model = await _wishlistService.GetWishlistAsync(CurrentUserId);
                return View(model);
            }

            // Guest Session Wishlist Fallback
            var guestModel = new WishlistViewModel();
            var ids = SessionWishlistIds;
            foreach (var id in ids)
            {
                var watch = await _catalogService.GetWatchByIdAsync(id);
                if (watch != null)
                {
                    guestModel.Items.Add(new WishlistItemViewModel
                    {
                        Id = watch.Id,
                        WatchId = watch.Id,
                        WatchName = watch.Name,
                        BrandName = watch.BrandName,
                        SKU = watch.SKU,
                        Price = watch.Price,
                        OriginalPrice = watch.OriginalPrice,
                        ImageUrl = watch.PrimaryImageUrl,
                        StockQuantity = watch.StockQuantity,
                        AddedDate = DateTime.UtcNow
                    });
                }
            }
            return View(guestModel);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Toggle(int watchId, string? returnUrl = null)
        {
            bool added;
            string message;
            int totalCount;

            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(CurrentUserId))
            {
                var result = await _wishlistService.ToggleWishlistAsync(CurrentUserId, watchId);
                added = result.Added;
                message = result.Message;
                totalCount = await _wishlistService.GetWishlistCountAsync(CurrentUserId);
            }
            else
            {
                var ids = SessionWishlistIds;
                var watch = await _catalogService.GetWatchByIdAsync(watchId);
                var name = watch?.Name ?? "Timepiece";

                if (ids.Contains(watchId))
                {
                    ids.Remove(watchId);
                    SessionWishlistIds = ids;
                    added = false;
                    message = $"Removed '{name}' from your wishlist.";
                }
                else
                {
                    ids.Add(watchId);
                    SessionWishlistIds = ids;
                    added = true;
                    message = $"Added '{name}' to your wishlist.";
                }
                totalCount = ids.Count;
            }

            TempData["SuccessMessage"] = message;

            // Support AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, added, message, count = totalCount });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Add(int watchId, string? returnUrl = null)
        {
            return await Toggle(watchId, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int watchId)
        {
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(CurrentUserId))
            {
                await _wishlistService.RemoveFromWishlistAsync(CurrentUserId, watchId);
            }
            else
            {
                var ids = SessionWishlistIds;
                ids.Remove(watchId);
                SessionWishlistIds = ids;
            }

            TempData["SuccessMessage"] = "Removed timepiece from your wishlist.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int watchId)
        {
            var (success, msg) = await _cartService.AddToCartAsync(CurrentUserId, SessionId, watchId, 1);
            if (success)
            {
                if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(CurrentUserId))
                {
                    await _wishlistService.RemoveFromWishlistAsync(CurrentUserId, watchId);
                }
                else
                {
                    var ids = SessionWishlistIds;
                    ids.Remove(watchId);
                    SessionWishlistIds = ids;
                }
                TempData["SuccessMessage"] = "Moved timepiece from wishlist to shopping cart.";
            }
            else
            {
                TempData["ErrorMessage"] = msg;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
