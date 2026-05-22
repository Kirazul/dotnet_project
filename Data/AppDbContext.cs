using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InvestPortfolio.Models;

namespace InvestPortfolio.Data
{
    // On hérite de IdentityDbContext (comme dans le TP11)
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Chaque DbSet = une Table SQL
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Budget> Budgets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Asset>().Property(a => a.UserId).IsRequired();
            builder.Entity<Asset>().HasIndex(a => a.UserId);

            builder.Entity<Budget>().Property(b => b.UserId).IsRequired();
            builder.Entity<Budget>().HasIndex(b => b.UserId);

            builder.Entity<Transaction>().Property(t => t.UserId).IsRequired();
            builder.Entity<Transaction>().HasIndex(t => t.UserId);

            builder.Entity<Tag>().Property(t => t.UserId).IsRequired();
            builder.Entity<Tag>().HasIndex(t => t.UserId);
        }
    }
}
