using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Services.Interfaces;

namespace TimeVerse.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
            return View(cart);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Add(int watchId, int quantity = 1, string? returnUrl = null)
        {
            var (success, message) = await _cartService.AddToCartAsync(CurrentUserId, SessionId, watchId, quantity);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                var cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
                return Json(new { success, message, totalCount = cart.TotalItemCount });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddToCart(int watchId, int quantity = 1, string? returnUrl = null)
        {
            return await Add(watchId, quantity, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int watchId, int quantity)
        {
            var (success, message) = await _cartService.UpdateQuantityAsync(CurrentUserId, SessionId, watchId, quantity);
            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int? watchId, int? cartItemId, int quantity)
        {
            var targetId = watchId ?? 0;
            if (targetId == 0 && cartItemId.HasValue)
            {
                var cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
                var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId.Value);
                if (item != null) targetId = item.WatchId;
            }
            return await Update(targetId, quantity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int? watchId, int? cartItemId)
        {
            var targetId = watchId ?? 0;
            if (targetId == 0 && cartItemId.HasValue)
            {
                var cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
                var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId.Value);
                if (item != null) targetId = item.WatchId;
            }
            await _cartService.RemoveFromCartAsync(CurrentUserId, SessionId, targetId);
            TempData["SuccessMessage"] = "Timepiece removed from shopping cart.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            await _cartService.ClearCartAsync(CurrentUserId, SessionId);
            TempData["SuccessMessage"] = "Shopping bag cleared.";
            return RedirectToAction(nameof(Index));
        }
    }
}
