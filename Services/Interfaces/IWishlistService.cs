using TimeVerse.ViewModels.Wishlist;

namespace TimeVerse.Services.Interfaces
{
    public interface IWishlistService
    {
        Task<WishlistViewModel> GetWishlistAsync(string userId);
        Task<(bool Added, string Message)> ToggleWishlistAsync(string userId, int watchId);
        Task<bool> RemoveFromWishlistAsync(string userId, int watchId);
        Task<int> GetWishlistCountAsync(string userId);
    }
}
