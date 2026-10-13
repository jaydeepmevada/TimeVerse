using TimeVerse.Models;
using TimeVerse.ViewModels;

namespace TimeVerse.Services.Interfaces
{
    public interface IWatchCatalogService
    {
        Task<HomeViewModel> GetHomeDataAsync();
        Task<WatchListViewModel> GetWatchesAsync(
            string? search,
            string? brand,
            string? category,
            MovementType? movement,
            WatchGender? gender,
            decimal? minPrice,
            decimal? maxPrice,
            string? stockFilter,
            string sort,
            int page,
            int pageSize);
        Task<WatchDetailsViewModel?> GetWatchByIdAsync(int id);
        Task<List<WatchCardViewModel>> GetRelatedWatchesAsync(int watchId, int count = 3);
        Task<WatchFilterOptions> GetFilterOptionsAsync();
        Task<bool> SubmitInquiryAsync(ContactViewModel inquiry);
    }
}
