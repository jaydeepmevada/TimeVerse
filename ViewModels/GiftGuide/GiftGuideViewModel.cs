namespace TimeVerse.ViewModels.GiftGuide
{
    public class GiftGuideViewModel
    {
        public List<WatchCardViewModel> GiftsForHim { get; set; } = new();
        public List<WatchCardViewModel> GiftsForHer { get; set; } = new();
        public List<WatchCardViewModel> MilestoneCelebrations { get; set; } = new();
        public List<WatchCardViewModel> HeirloomInvestments { get; set; } = new();
        public List<WatchCardViewModel> UnderFiftyThousand { get; set; } = new();
    }
}
