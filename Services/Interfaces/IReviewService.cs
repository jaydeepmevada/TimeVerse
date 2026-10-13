using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.ViewModels;

namespace TimeVerse.Services.Interfaces
{
    public interface IReviewService
    {
        Task<(bool Success, string Message)> AddReviewAsync(int watchId, string userId, int rating, string comment);
        Task<List<CustomerReviewItem>> GetWatchReviewsAsync(int watchId);
        Task<bool> CanUserReviewWatchAsync(int watchId, string userId);
    }
}
