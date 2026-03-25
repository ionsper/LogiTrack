using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LogiTrack.Models;

namespace LogiTrack.Data
{
    /// <summary>
    /// EF Core DbContext for the application. Inherits from IdentityDbContext to include
    /// ASP.NET Core Identity tables and configuration.
    /// </summary>
    public class LogiTrackContext : IdentityDbContext<ApplicationUser>
    {
        public LogiTrackContext(DbContextOptions<LogiTrackContext> options) : base(options) { }

        /// <summary>
        /// Inventory items available in the system.
        /// </summary>
        public DbSet<InventoryItem> InventoryItems { get; set; }

        /// <summary>
        /// Customer orders with their associated inventory items.
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        // Remove OnConfiguring, configuration is now handled by DI

        /// <summary>
        /// Configure entity relationships and behavior on model creation.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}