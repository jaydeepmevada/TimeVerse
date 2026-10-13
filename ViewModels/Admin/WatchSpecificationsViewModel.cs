using System.ComponentModel.DataAnnotations;
using TimeVerse.Models;

namespace TimeVerse.ViewModels.Admin
{
    public class WatchSpecificationsViewModel
    {
        public int WatchId { get; set; }
        public string WatchName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public bool IsNew { get; set; }

        // Common specifications (Non-mandatory, pre-filled with common defaults)
        [Display(Name = "Movement / Calibre")]
        public MovementType MovementType { get; set; } = MovementType.Automatic;

        [Display(Name = "Gender / Target Fit")]
        public WatchGender Gender { get; set; } = WatchGender.Unisex;

        [Display(Name = "Case Diameter / Size")]
        [StringLength(50)]
        public string CaseSize { get; set; } = "40mm";

        [Display(Name = "Case Material")]
        [StringLength(100)]
        public string CaseMaterial { get; set; } = "Stainless Steel";

        [Display(Name = "Dial Color & Finish")]
        [StringLength(50)]
        public string DialColor { get; set; } = "Black";

        [Display(Name = "Strap / Bracelet Material")]
        [StringLength(100)]
        public string StrapMaterial { get; set; } = "Stainless Steel";

        [Display(Name = "Water Resistance")]
        [StringLength(50)]
        public string WaterResistance { get; set; } = "100m / 10 ATM";

        [Display(Name = "Warranty Coverage")]
        [StringLength(100)]
        public string Warranty { get; set; } = "2 Years International";

        [Display(Name = "Country of Origin")]
        [StringLength(100)]
        public string CountryOfOrigin { get; set; } = "Switzerland";

        // Optional Badges
        [Display(Name = "Featured Watch")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Best Seller")]
        public bool IsBestSeller { get; set; }

        [Display(Name = "New Arrival")]
        public bool IsNewArrival { get; set; } = true;

        [Display(Name = "Limited Edition")]
        public bool IsLimitedEdition { get; set; }
    }
}
