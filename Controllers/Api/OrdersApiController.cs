using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Data;
using TimeVerse.DTOs;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Orders;

namespace TimeVerse.Controllers.Api
{
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrdersApiController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersApiController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Public endpoint to track an order by its reference code (e.g. TV-20260916-8001).
        /// </summary>
        [HttpGet("track/{orderNumber}")]
        [ProducesResponseType(typeof(OrderTrackingResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderTrackingResponseDto>> TrackOrder(string orderNumber)
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber.Trim());
            if (order == null)
            {
                return NotFound(new { message = $"No order found matching reference '{orderNumber}'." });
            }

            return Ok(new OrderTrackingResponseDto
            {
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TrackingNumber = order.TrackingNumber,
                FinalAmount = order.FinalAmount,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ItemCount = order.Items.Sum(i => i.Quantity)
            });
        }

        /// <summary>
        /// Retrieves details of an order by its primary ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(OrderDetailsViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDetailsViewModel>> GetOrderById(int id)
        {
            var isAdminOrSales = User.IsInRole(RoleConstants.Admin) || User.IsInRole(RoleConstants.SalesPerson);
            var order = await _orderService.GetOrderDetailsAsync(id, isAdminOrSales ? null : CurrentUserId);
            if (order == null)
            {
                return NotFound(new { message = $"Order #{id} was not found or access is restricted." });
            }

            return Ok(order);
        }

        /// <summary>
        /// Retrieves past orders placed by the currently authenticated customer.
        /// </summary>
        [HttpGet("my-orders")]
        [Authorize]
        [ProducesResponseType(typeof(List<OrderSummaryViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<OrderSummaryViewModel>>> GetMyOrders()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Unauthorized();
            }

            var orders = await _orderService.GetCustomerOrdersAsync(CurrentUserId);
            return Ok(orders);
        }

        /// <summary>
        /// Retrieves all orders in the vault queue (Sales &amp; Admin only).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.SalesPerson}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderStatus? status)
        {
            var orders = await _orderService.GetAllOrdersAsync(status);
            return Ok(orders);
        }

        /// <summary>
        /// Updates the fulfillment status and courier tracking of an order.
        /// </summary>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = $"{RoleConstants.Admin},{RoleConstants.SalesPerson}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] OrderStatus status, [FromQuery] string? trackingNumber)
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, status, trackingNumber);
            if (!success) return BadRequest(new { message = $"Failed to update status for order #{id}." });

            return Ok(new { success = true, orderId = id, newStatus = status.ToString(), trackingNumber });
        }
    }
}
