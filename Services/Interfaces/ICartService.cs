using TimeVerse.ViewModels.Cart;

namespace TimeVerse.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync(string? userId, string? sessionId);
        Task<(bool Success, string Message)> AddToCartAsync(string? userId, string? sessionId, int watchId, int quantity = 1);
        Task<(bool Success, string Message)> UpdateQuantityAsync(string? userId, string? sessionId, int watchId, int newQuantity);
        Task<bool> RemoveFromCartAsync(string? userId, string? sessionId, int watchId);
        Task ClearCartAsync(string? userId, string? sessionId);
        Task<int> GetCartCountAsync(string? userId, string? sessionId);
    }
}
