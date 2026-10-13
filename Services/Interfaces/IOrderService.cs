using TimeVerse.Models;
using TimeVerse.ViewModels.Checkout;
using TimeVerse.ViewModels.Orders;

namespace TimeVerse.Services.Interfaces
{
    public interface IOrderService
    {
        Task<(bool Success, string Message, string? OrderNumber)> CreateOrderAsync(
            string userId,
            CheckoutViewModel checkoutModel,
            string? sessionId);

        Task<List<OrderSummaryViewModel>> GetCustomerOrdersAsync(string userId);
        Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId, string? userId = null);
        Task<OrderDetailsViewModel?> GetOrderByNumberAsync(string orderNumber, string? userId = null);
        Task<List<OrderDetailsViewModel>> GetAllOrdersAsync(OrderStatus? status = null);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? trackingNumber = null);
    }
}
