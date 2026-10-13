using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;

namespace TimeVerse.Areas.Sales.Controllers
{
    [Area("Sales")]
    [Authorize(Roles = $"{RoleConstants.SalesPerson},{RoleConstants.Admin}")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(OrderStatus? status)
        {
            ViewBag.CurrentStatus = status;
            var orders = await _orderService.GetAllOrdersAsync(status);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus, string? trackingNumber)
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, newStatus, trackingNumber);
            if (success)
            {
                TempData["SuccessMessage"] = $"Order #{id} status updated to {newStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not update order status. Please verify order exists.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
