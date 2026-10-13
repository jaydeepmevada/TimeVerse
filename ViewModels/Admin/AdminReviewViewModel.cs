namespace TimeVerse.ViewModels.Admin
{
    public class AdminReviewItemViewModel
    {
        public int Id { get; set; }
        public int WatchId { get; set; }
        public string WatchName { get; set; } = string.Empty;
        public string WatchSKU { get; set; } = string.Empty;
        public string WatchImageUrl { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminReviewListViewModel
    {
        public List<AdminReviewItemViewModel> Reviews { get; set; } = new();
        public string? Search { get; set; }
        public bool? ApprovalFilter { get; set; }
        public int TotalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
    }
}
