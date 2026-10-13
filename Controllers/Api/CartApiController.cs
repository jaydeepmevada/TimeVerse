using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.DTOs;
using TimeVerse.Services.Interfaces;

namespace TimeVerse.Controllers.Api
{
    [ApiController]
    [Route("api/cart")]
    [Produces("application/json")]
    public class CartApiController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartApiController(ICartService cartService)
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

        /// <summary>
        /// Retrieves the current user's shopping vault state and item counts.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(CartSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CartSummaryDto>> GetCart()
        {
            var cart = await _cartService.GetCartAsync(CurrentUserId, SessionId);
            return Ok(new CartSummaryDto
            {
                TotalItemCount = cart.TotalItemCount,
                Subtotal = cart.Subtotal,
                ShippingFee = cart.ShippingFee,
                GrandTotal = cart.GrandTotal,
                QualifiesForFreeShipping = cart.QualifiesForFreeShipping,
                Items = cart.Items.Select(i => new CartItemDto
                {
                    WatchId = i.WatchId,
                    WatchName = i.WatchName,
                    SKU = i.SKU,
                    ImageUrl = i.ImageUrl,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice
                }).ToList()
            });
        }

        /// <summary>
        /// Asynchronously adds a timepiece to the shopping vault.
        /// </summary>
        [HttpPost("items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItem([FromBody] CartItemAddDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message) = await _cartService.AddToCartAsync(CurrentUserId, SessionId, dto.WatchId, dto.Quantity);
            if (!success) return BadRequest(new { message });

            var count = await _cartService.GetCartCountAsync(CurrentUserId, SessionId);
            return Ok(new { success = true, message, totalCount = count });
        }

        /// <summary>
        /// Asynchronously updates timepiece quantity in vault.
        /// </summary>
        [HttpPut("items/{watchId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateItemQuantity(int watchId, [FromBody] CartItemUpdateDto dto)
        {
            if (dto.Quantity <= 0)
            {
                await _cartService.RemoveFromCartAsync(CurrentUserId, SessionId, watchId);
                var count = await _cartService.GetCartCountAsync(CurrentUserId, SessionId);
                return Ok(new { success = true, message = "Item removed", totalCount = count });
            }

            var (success, message) = await _cartService.UpdateQuantityAsync(CurrentUserId, SessionId, watchId, dto.Quantity);
            if (!success) return BadRequest(new { message });

            var totalCount = await _cartService.GetCartCountAsync(CurrentUserId, SessionId);
            return Ok(new { success = true, message, totalCount });
        }

        /// <summary>
        /// Removes an item from the shopping vault.
        /// </summary>
        [HttpDelete("items/{watchId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveItem(int watchId)
        {
            await _cartService.RemoveFromCartAsync(CurrentUserId, SessionId, watchId);
            var count = await _cartService.GetCartCountAsync(CurrentUserId, SessionId);
            return Ok(new { success = true, message = "Timepiece removed from vault.", totalCount = count });
        }
    }
}
