using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Models;

namespace TimeVerse.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Watch> Watches => Set<Watch>();
        public DbSet<WatchImage> WatchImages => Set<WatchImage>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Testimonial> Testimonials => Set<Testimonial>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Decimal Precisions
            builder.Entity<Watch>()
                .Property(w => w.Price)
                .HasPrecision(18, 2);

            builder.Entity<Watch>()
                .Property(w => w.OriginalPrice)
                .HasPrecision(18, 2);

            builder.Entity<Watch>()
                .Property(w => w.DiscountPercentage)
                .HasPrecision(5, 2);

            builder.Entity<Watch>()
                .Property(w => w.Rating)
                .HasPrecision(3, 2);

            builder.Entity<CartItem>()
                .Property(c => c.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.ShippingFee)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.FinalAmount)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(18, 2);

            // Indexes & Unique Constraints
            builder.Entity<Watch>()
                .HasIndex(w => w.SKU)
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            builder.Entity<Brand>()
                .HasIndex(b => b.Name)
                .IsUnique();

            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Relationships
            builder.Entity<Watch>()
                .HasOne(w => w.Brand)
                .WithMany(b => b.Watches)
                .HasForeignKey(w => w.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Watch>()
                .HasOne(w => w.Category)
                .WithMany(c => c.Watches)
                .HasForeignKey(w => w.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WatchImage>()
                .HasOne(i => i.Watch)
                .WithMany(w => w.Images)
                .HasForeignKey(i => i.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Address>()
                .HasOne(a => a.ApplicationUser)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Watch)
                .WithMany()
                .HasForeignKey(oi => oi.WatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Watch)
                .WithMany()
                .HasForeignKey(r => r.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne(w => w.Watch)
                .WithMany()
                .HasForeignKey(w => w.WatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(c => c.Watch)
                .WithMany()
                .HasForeignKey(c => c.WatchId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
