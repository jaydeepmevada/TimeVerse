using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;

namespace TimeVerse.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleConstants.Customer)]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(
            IOrderService orderService,
            UserManager<ApplicationUser> userManager)
        {
            _orderService = orderService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _orderService.GetCustomerOrdersAsync(user.Id);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _orderService.GetOrderDetailsAsync(id, user.Id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _orderService.GetOrderDetailsAsync(id, user.Id);
            if (order == null) return NotFound();

            return View("~/Views/Orders/Invoice.cshtml", order);
        }
    }
}
