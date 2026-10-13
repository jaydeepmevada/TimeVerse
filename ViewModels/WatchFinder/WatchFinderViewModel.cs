using TimeVerse.Models;

namespace TimeVerse.ViewModels.WatchFinder
{
    public class WatchFinderWizardViewModel
    {
        public WatchGender? SelectedGender { get; set; }
        public string? Lifestyle { get; set; } // "luxury", "daily", "racing", "smart"
        public string? BudgetRange { get; set; } // "under30k", "30k-100k", "above100k"
        public List<WatchCardViewModel> RecommendedWatches { get; set; } = new();
        public bool HasSearched { get; set; } = false;
    }
}
