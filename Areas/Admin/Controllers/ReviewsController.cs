using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.ViewModels.Admin;

namespace TimeVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, bool? isApproved)
        {
            var query = _context.Reviews
                .Include(r => r.Watch)
                    .ThenInclude(w => w!.Images)
                .Include(r => r.User)
                .AsQueryable();

            if (isApproved.HasValue)
            {
                query = query.Where(r => r.IsApproved == isApproved.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(r =>
                    r.Comment.ToLower().Contains(s) ||
                    (r.Watch != null && (r.Watch.Name.ToLower().Contains(s) || r.Watch.SKU.ToLower().Contains(s))) ||
                    (r.User != null && ((r.User.Email != null && r.User.Email.ToLower().Contains(s)) || r.User.FirstName.ToLower().Contains(s) || r.User.LastName.ToLower().Contains(s))));
            }

            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var model = new AdminReviewListViewModel
            {
                Search = search,
                ApprovalFilter = isApproved,
                TotalCount = await _context.Reviews.CountAsync(),
                ApprovedCount = await _context.Reviews.CountAsync(r => r.IsApproved),
                PendingCount = await _context.Reviews.CountAsync(r => !r.IsApproved),
                Reviews = reviews.Select(r => new AdminReviewItemViewModel
                {
                    Id = r.Id,
                    WatchId = r.WatchId,
                    WatchName = r.Watch?.Name ?? "Timepiece Reference",
                    WatchSKU = r.Watch?.SKU ?? "N/A",
                    WatchImageUrl = r.Watch?.Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath ?? "/images/watches/placeholder.svg",
                    UserId = r.UserId,
                    UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}".Trim() : "Collector",
                    UserEmail = r.User?.Email ?? string.Empty,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsApproved = r.IsApproved,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = !review.IsApproved;
            await _context.SaveChangesAsync();

            // Recalculate watch rating
            await RecalculateWatchRatingAsync(review.WatchId);

            TempData["SuccessMessage"] = review.IsApproved
                ? "Review has been approved and published to public storefront."
                : "Review has been unapproved and hidden from public storefront.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            var watchId = review.WatchId;
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Recalculate watch rating
            await RecalculateWatchRatingAsync(watchId);

            TempData["SuccessMessage"] = "Review removed permanently.";
            return RedirectToAction(nameof(Index));
        }

        private async Task RecalculateWatchRatingAsync(int watchId)
        {
            var watch = await _context.Watches.FindAsync(watchId);
            if (watch == null) return;

            var approved = await _context.Reviews
                .Where(r => r.WatchId == watchId && r.IsApproved)
                .ToListAsync();

            watch.ReviewCount = approved.Count;
            watch.Rating = approved.Any() ? (decimal)approved.Average(r => r.Rating) : 5.0m;
            await _context.SaveChangesAsync();
        }
    }
}
