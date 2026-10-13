using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels;

namespace TimeVerse.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> AddReviewAsync(int watchId, string userId, int rating, string comment)
        {
            var watch = await _context.Watches.FindAsync(watchId);
            if (watch == null)
            {
                return (false, "Timepiece reference not found.");
            }

            if (rating < 1 || rating > 5)
            {
                return (false, "Rating must be between 1 and 5 stars.");
            }

            // Check if patron already reviewed this watch
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.WatchId == watchId && r.UserId == userId);
            if (existingReview != null)
            {
                return (false, "You have already submitted a review for this timepiece reference.");
            }

            // Check Verified Purchaser Rule: Has this user had an order with status 'Delivered' containing this watch?
            var isVerified = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.OrderItems)
                .AnyAsync(oi => oi.WatchId == watchId);

            var review = new Review
            {
                WatchId = watchId,
                UserId = userId,
                Rating = rating,
                Comment = comment.Trim(),
                IsApproved = true,
                IsVerifiedPurchase = isVerified,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Recalculate watch rating and count
            var approvedReviews = await _context.Reviews
                .Where(r => r.WatchId == watchId && r.IsApproved)
                .ToListAsync();

            watch.ReviewCount = approvedReviews.Count;
            watch.Rating = approvedReviews.Any() ? (decimal)approvedReviews.Average(r => r.Rating) : 5.0m;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Review added for watch {WatchId} by user {UserId}. Verified: {IsVerified}.", watchId, userId, isVerified);
            return (true, isVerified 
                ? "Your verified purchaser review has been published." 
                : "Thank you for your horology review.");
        }

        public async Task<List<CustomerReviewItem>> GetWatchReviewsAsync(int watchId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.WatchId == watchId && r.IsApproved)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new CustomerReviewItem
            {
                AuthorName = r.User != null ? $"{r.User.FirstName} {r.User.LastName.FirstOrDefault()}." : "Collector",
                Rating = r.Rating,
                Comment = r.Comment,
                DateAgo = GetRelativeDate(r.CreatedAt),
                IsVerifiedPurchaser = r.IsVerifiedPurchase
            }).ToList();
        }

        public async Task<bool> CanUserReviewWatchAsync(int watchId, string userId)
        {
            return !await _context.Reviews.AnyAsync(r => r.WatchId == watchId && r.UserId == userId);
        }

        private string GetRelativeDate(DateTime dt)
        {
            var diff = DateTime.UtcNow - dt;
            if (diff.TotalDays < 1) return "today";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
            if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} weeks ago";
            return dt.ToString("dd MMM yyyy");
        }
    }
}
