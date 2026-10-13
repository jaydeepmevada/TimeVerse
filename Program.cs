using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Implementations;
using TimeVerse.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=TimeVerse.db");
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=.\\SQLEXPRESS;Database=TimeVerseDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}

// 2. ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password Security Policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Cookie Policy & Route Redirection
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.SlidingExpiration = true;
});

// 4. Distributed Cache & Session Management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TimeVerse.Session";
});
builder.Services.AddHttpContextAccessor();

// 5. MVC & Razor Configuration
builder.Services.AddControllersWithViews();

// Register application services
builder.Services.AddScoped<IWatchCatalogService, WatchCatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Configure lower-case URLs
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// 6. Swagger & OpenAPI Specification
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "TimeVerse Luxury Horology REST API",
        Version = "v1",
        Description = "Enterprise RESTful Web API for TimeVerse Watches E-Commerce Platform. Provides endpoints for catalog discovery, shopping vault, orders fulfillment, and inventory governance.",
        Contact = new Microsoft.OpenApi.OpenApiContact
        {
            Name = "TimeVerse Horology Engineering",
            Email = "api@timeverse.com",
            Url = new Uri("https://github.com/timeverse")
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 7. Health Checks
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// 5. Seed Identity Roles and Default Administrators on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var config = services.GetRequiredService<IConfiguration>();
    try
    {
        await DbSeeder.SeedRolesAndUsersAsync(services, config);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An exception occurred while seeding initial roles and users.");
    }
}

// 6. HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCodeHandler", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Swagger Interactive Documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TimeVerse REST API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "TimeVerse API Documentation - Swagger UI";
});

app.UseRouting();

// Health Check Endpoint
app.MapHealthChecks("/health");

// Session Middleware
app.UseSession();

// Authentication MUST precede Authorization
app.UseAuthentication();
app.UseAuthorization();

// Attribute-routed API Controllers
app.MapControllers();

// Area routing (Admin, Sales, Customer)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Friendly SEO routes
app.MapControllerRoute(
    name: "shop",
    pattern: "shop",
    defaults: new { controller = "Shop", action = "Index" });

app.MapControllerRoute(
    name: "shop_details",
    pattern: "shop/details/{id:int}",
    defaults: new { controller = "Shop", action = "Details" });

app.MapControllerRoute(
    name: "categories",
    pattern: "categories",
    defaults: new { controller = "Shop", action = "Index" });

app.MapControllerRoute(
    name: "privacy",
    pattern: "privacy",
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "terms",
    pattern: "terms",
    defaults: new { controller = "Home", action = "Terms" });

app.MapControllerRoute(
    name: "help",
    pattern: "help",
    defaults: new { controller = "Home", action = "Help" });

app.MapControllerRoute(
    name: "watches_details",
    pattern: "watches/details/{id:int}",
    defaults: new { controller = "Watches", action = "Details" });

app.MapControllerRoute(
    name: "watches_catalog",
    pattern: "watches",
    defaults: new { controller = "Watches", action = "Index" });

app.MapControllerRoute(
    name: "cart",
    pattern: "cart",
    defaults: new { controller = "Cart", action = "Index" });

app.MapControllerRoute(
    name: "checkout",
    pattern: "checkout",
    defaults: new { controller = "Checkout", action = "Index" });

app.MapControllerRoute(
    name: "wishlist",
    pattern: "wishlist",
    defaults: new { controller = "Wishlist", action = "Index" });

app.MapControllerRoute(
    name: "compare",
    pattern: "compare",
    defaults: new { controller = "Compare", action = "Index" });

app.MapControllerRoute(
    name: "watchfinder",
    pattern: "watch-finder",
    defaults: new { controller = "WatchFinder", action = "Index" });

app.MapControllerRoute(
    name: "giftguide",
    pattern: "gift-guide",
    defaults: new { controller = "GiftGuide", action = "Index" });

app.MapControllerRoute(
    name: "about",
    pattern: "about",
    defaults: new { controller = "About", action = "Index" });

app.MapControllerRoute(
    name: "contact",
    pattern: "contact",
    defaults: new { controller = "Contact", action = "Index" });

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
