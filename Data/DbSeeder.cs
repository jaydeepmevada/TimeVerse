using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Models;

namespace TimeVerse.Data
{
    public static class RoleConstants
    {
        public const string Admin = "Admin";
        public const string SalesPerson = "SalesPerson";
        public const string Customer = "Customer";
    }

    public static class DbSeeder
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Ensure database is created
            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbSeeder Notice] Database connection/EnsureCreated: {ex.Message}");
            }

            // 2. Ensure standard Identity Roles exist
            string[] roles = { RoleConstants.Admin, RoleConstants.SalesPerson, RoleConstants.Customer };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Ensure Primary Administrator Account exists
            var adminEmail = configuration["TIMEVERSE_ADMIN_EMAIL"] ?? "admin@timeverse.com";
            var adminPassword = configuration["TIMEVERSE_ADMIN_PASSWORD"] ?? "Admin@2005";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Alexander",
                    LastName = "Vance",
                    PhoneNumber = "+91 98000 11111",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
                }
            }
            else
            {
                // Ensure admin password matches updated password
                var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
                await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
            }

            // Note: One-time purge of demo data has completed. Future additions by users will persist permanently across all restarts.

            // 7. Seed Standard Brands if empty (for dropdown selection in Add Watch form)
            try
            {
                if (!await context.Brands.AnyAsync())
                {
                    var brands = new List<Brand>
                    {
                        new() { Name = "Rolex", Description = "Synonym of Swiss excellence and horological prestige." },
                        new() { Name = "Omega", Description = "Pioneering spirit, moon missions, and precision oceanography." },
                        new() { Name = "Seiko", Description = "Japanese mastery of mechanical precision and Spring Drive technology." },
                        new() { Name = "Titan", Description = "Indian design sophistication, Edge slimness, and timeless elegance." },
                        new() { Name = "Casio", Description = "Indestructible toughness and modern digital-analog connectivity." },
                        new() { Name = "Fossil", Description = "Vintage American authenticity with modern mechanical flair." }
                    };
                    context.Brands.AddRange(brands);
                    await context.SaveChangesAsync();
                }

                // 8. Seed & Sync Strictly 5 approved Categories
                await SyncCategoriesAndWatchesAsync(context);

                // 9. Restore user-added watches with their original uploaded photos
                await RestoreUserWatchesAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbSeeder Warning] Brands/Categories/Watches sync notice: {ex.Message}");
            }
        }

        private static async Task SyncCategoriesAndWatchesAsync(ApplicationDbContext context)
        {
            var desiredCategories = new (string Name, string Description)[]
            {
                ("Men's Watches", "Bold masculine silhouettes crafted for modern gentlemen."),
                ("Women's Watches", "Graceful precision with bespoke diamond-cut accents."),
                ("Smart Watches", "Connected luxury with biometric health and notifications."),
                ("Digital Watches", "Multi-functional digital displays with precision timekeeping & toughness."),
                ("Chronograph Watches", "Precision stopwatch complication for sporting milestones.")
            };

            var allowedNames = desiredCategories.Select(d => d.Name).ToList();

            foreach (var (name, desc) in desiredCategories)
            {
                var existing = await context.Categories.FirstOrDefaultAsync(c => c.Name == name);
                if (existing == null)
                {
                    if (name == "Digital Watches")
                    {
                        var oldAuto = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Automatic Watches" || c.Name == "Automatic");
                        if (oldAuto != null)
                        {
                            oldAuto.Name = "Digital Watches";
                            oldAuto.Description = desc;
                            oldAuto.IsActive = true;
                            continue;
                        }
                    }

                    var shortName = name.Replace(" Watches", "");
                    var oldCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == shortName);
                    if (oldCat != null)
                    {
                        oldCat.Name = name;
                        oldCat.Description = desc;
                        oldCat.IsActive = true;
                    }
                    else
                    {
                        context.Categories.Add(new Category { Name = name, Description = desc, IsActive = true });
                    }
                }
                else
                {
                    existing.IsActive = true;
                }
            }
            await context.SaveChangesAsync();

            // Remove any obsolete categories not in the 5 approved list
            var obsoleteCategories = await context.Categories
                .Where(c => !allowedNames.Contains(c.Name))
                .ToListAsync();

            if (obsoleteCategories.Any())
            {
                context.Categories.RemoveRange(obsoleteCategories);
                await context.SaveChangesAsync();
            }
        }

        private static async Task RestoreUserWatchesAsync(ApplicationDbContext context)
        {
            // 1. Ensure Fastrack brand exists
            var fastrack = await context.Brands.FirstOrDefaultAsync(b => b.Name == "Fastrack");
            if (fastrack == null)
            {
                context.Brands.Add(new Brand
                {
                    Name = "Fastrack",
                    Description = "Youthful and vibrant fashion accessories and lifestyle timepieces.",
                    LogoPath = "/images/brands/fastrack.png",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(fastrack.LogoPath))
            {
                fastrack.LogoPath = "/images/brands/fastrack.png";
                await context.SaveChangesAsync();
            }

            // 2. Only restore watches if database is currently empty
            if (await context.Watches.AnyAsync())
            {
                return;
            }

            var brandMap = await context.Brands.ToDictionaryAsync(b => b.Name, b => b.Id);
            var catMap = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

            void AddWatch(
                string name,
                string brandName,
                string catName,
                decimal price,
                decimal origPrice,
                int stock,
                string imagePath,
                string sku,
                string desc,
                MovementType movement,
                WatchGender gender)
            {
                if (!brandMap.TryGetValue(brandName, out var brandId)) return;
                if (!catMap.TryGetValue(catName, out var catId)) return;

                var w = new Watch
                {
                    Name = name,
                    BrandId = brandId,
                    CategoryId = catId,
                    Price = price,
                    OriginalPrice = origPrice,
                    DiscountPercentage = origPrice > price ? Math.Round(((origPrice - price) / origPrice) * 100, 0) : 0,
                    StockQuantity = stock,
                    SKU = sku,
                    Description = desc,
                    MovementType = movement,
                    Gender = gender,
                    IsActive = true,
                    IsNewArrival = true,
                    CaseSize = "40mm",
                    CaseMaterial = "Stainless Steel",
                    DialColor = "Black",
                    StrapMaterial = "Stainless Steel",
                    WaterResistance = "100m / 10 ATM",
                    Warranty = "2 Years International",
                    CountryOfOrigin = brandName == "Casio" || brandName == "Seiko" ? "Japan" : (brandName == "Titan" || brandName == "Fastrack" ? "India" : "Switzerland"),
                    CreatedAt = DateTime.UtcNow
                };

                w.Images.Add(new WatchImage
                {
                    ImagePath = imagePath,
                    IsPrimary = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                });

                context.Watches.Add(w);
            }

            // Restore user's 15 watches matching their exact uploaded photos
            AddWatch("Casio Men Analog Watch", "Casio", "Men's Watches", 3995, 4995, 15, "/uploads/watches/9e566e35-073e-4226-bb8b-049f2b01c6bc.jpg", "CAS-ANA-01", "Classic Casio analog timepiece for men with durable stainless steel case and quartz precision.", MovementType.Quartz, WatchGender.Men);
            AddWatch("Casio Chronograph Watch", "Casio", "Chronograph Watches", 7495, 8995, 12, "/uploads/watches/f0672358-2604-4883-8290-61c62e5a8894.jpg", "CAS-CHR-02", "High-performance Casio chronograph featuring sub-dials, stopwatch function, and sporty bezel.", MovementType.Quartz, WatchGender.Men);
            AddWatch("Casio Women Digital Watch", "Casio", "Digital Watches", 2995, 3495, 18, "/uploads/watches/ebbc4237-091f-4d08-97c5-cedfac180a63.jpg", "CAS-DIG-03", "Chic and compact Casio digital watch designed for women with retro vintage charm and daily alarm.", MovementType.Quartz, WatchGender.Women);
            AddWatch("Casio G-shock Chronograph Watch", "Casio", "Chronograph Watches", 8495, 9995, 10, "/uploads/watches/b37e34f2-4b3b-4d71-a781-77b46d911a49.jpg", "CAS-GSH-04", "Indestructible Casio G-Shock chronograph with rugged shock resistance and 200m water capability.", MovementType.Quartz, WatchGender.Men);
            AddWatch("Casio Digital Watch", "Casio", "Digital Watches", 1995, 2495, 25, "/uploads/watches/b4638ace-5128-4dfa-92f1-ae366bde4d79.jpg", "CAS-DIG-05", "Iconic Casio classic digital timekeeper with electro-luminescent backlight and perpetual calendar.", MovementType.Quartz, WatchGender.Unisex);
            AddWatch("Rolex Date&Time Watch", "Rolex", "Men's Watches", 795000, 850000, 3, "/uploads/watches/e33f6ca5-a6a8-457c-b8e6-5c7b4f844b2a.jpg", "ROL-DAT-06", "Prestige Rolex Datejust timepiece crafted with fluted bezel, cyclops lens, and Jubilee bracelet.", MovementType.Automatic, WatchGender.Men);
            AddWatch("Rolex Women Watch", "Rolex", "Women's Watches", 680000, 720000, 2, "/uploads/watches/1aad7cc9-45e0-41fb-b4d5-83b6860c4ba4.webp", "ROL-WOM-07", "Exquisite Rolex Lady timepiece with lustrous sunray dial and oyster steel engineering.", MovementType.Automatic, WatchGender.Women);
            AddWatch("Omega Chronograph Watch", "Omega", "Chronograph Watches", 485000, 520000, 4, "/uploads/watches/062e9e4a-8907-43bf-97db-661d3ccb81d7.jpg", "OMG-CHR-08", "Luxury Omega Seamaster chronograph with coaxial master chronometer movement and ceramic bezel.", MovementType.Automatic, WatchGender.Men);
            AddWatch("Omega Speedmaster Watch", "Omega", "Chronograph Watches", 575000, 610000, 3, "/uploads/watches/5f6040aa-93eb-4697-baba-f31b916a5e93.jpg", "OMG-SPE-09", "The legendary Omega Speedmaster Professional Moonwatch tested by astronauts in lunar exploration.", MovementType.Automatic, WatchGender.Men);
            AddWatch("Seiko Chronograph Watch", "Seiko", "Chronograph Watches", 24900, 28500, 8, "/uploads/watches/e30d2379-84b9-4ea7-821a-795347f48193.jpg", "SEI-CHR-10", "Masterfully tuned Seiko chronograph featuring solar charging, tachymeter, and lumibrite markers.", MovementType.Quartz, WatchGender.Men);
            AddWatch("Fastrack Women Watch", "Fastrack", "Women's Watches", 2495, 2995, 15, "/uploads/watches/e1e0c5ba-fa64-4563-8ab6-26f31ffc4f1f.jpg", "FAS-WOM-11", "Contemporary Fastrack women's analog watch featuring minimalist aesthetic and sleek metallic bracelet.", MovementType.Quartz, WatchGender.Women);
            AddWatch("Fastrack Men Smart Watch", "Fastrack", "Smart Watches", 3295, 3995, 20, "/uploads/watches/a1602b58-5908-4c67-b0b7-ce0b3099756d.jpg", "FAS-SMA-12", "Modern Fastrack smartwatch with vibrant HD touchscreen display, Bluetooth calling, and multisport modes.", MovementType.Smart, WatchGender.Men);
            AddWatch("Fastrack Women Casual Watch", "Fastrack", "Women's Watches", 1995, 2495, 18, "/uploads/watches/608eb8e0-0f95-45a2-a276-0aaa53323044.webp", "FAS-CAS-13", "Casual Fastrack watch for women with colorful dial and durable comfortable strap.", MovementType.Quartz, WatchGender.Women);
            AddWatch("Titan Men's Watch", "Titan", "Men's Watches", 5995, 6995, 14, "/uploads/watches/e9aa0407-aa57-4044-bccb-bbb2da9f6d20.webp", "TIT-MEN-14", "Titan Edge ultra-slim luxury watch for men with architectural dial styling and genuine leather strap.", MovementType.Quartz, WatchGender.Men);
            AddWatch("Titan Chronograph Watch", "Titan", "Chronograph Watches", 9995, 11995, 10, "/uploads/watches/891e83fc-17ef-472a-970c-20dd439327ba.webp", "TIT-CHR-15", "Bold Titan Octane chronograph featuring multi-layer dial, tachymeter scale, and 100m water resistance.", MovementType.Quartz, WatchGender.Men);

            await context.SaveChangesAsync();
            Console.WriteLine("[DbSeeder] All 15 user-added watches have been successfully restored with their uploaded photos.");
        }
    }
}
