using TimeVerse.Models;

namespace TimeVerse.ViewModels.Compare
{
    public class CompareItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public MovementType MovementType { get; set; }
        public string CaseSize { get; set; } = string.Empty;
        public string CaseMaterial { get; set; } = string.Empty;
        public string DialColor { get; set; } = string.Empty;
        public string StrapMaterial { get; set; } = string.Empty;
        public string WaterResistance { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public bool InStock { get; set; }
    }

    public class CompareViewModel
    {
        public List<CompareItemViewModel> Watches { get; set; } = new();
    }
}
