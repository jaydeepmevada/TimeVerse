using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Customer;

namespace TimeVerse.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleConstants.Customer)]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ICartService cartService,
            IWishlistService wishlistService)
        {
            _userManager = userManager;
            _context = context;
            _cartService = cartService;
            _wishlistService = wishlistService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);
            var wishlistCount = await _wishlistService.GetWishlistCountAsync(user.Id);
            var cartCount = await _cartService.GetCartCountAsync(user.Id, null);
            var addressCount = await _context.Addresses.CountAsync(a => a.ApplicationUserId == user.Id);

            var isNewUser = (DateTime.UtcNow - user.CreatedAt).TotalHours < 48;

            var model = new CustomerDashboardViewModel
            {
                FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : "Customer",
                Email = user.Email ?? string.Empty,
                OrderCount = orderCount,
                WishlistCount = wishlistCount,
                CartCount = cartCount,
                AddressCount = addressCount,
                IsNewUser = isNewUser
            };

            return View(model);
        }
    }
}
