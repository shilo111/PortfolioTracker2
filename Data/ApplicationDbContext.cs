using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Models;

namespace PortfolioTracker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                "Server=localhost;Database=portfolio;User=root;Password=dadon56091;",
                new MySqlServerVersion(new Version(8, 0, 28)) // עדכן את גרסת MySQL
            );
        }


        // הוסף כאן DbSet עבור הטבלאות שלך
        public DbSet<MarketReturn> MarketReturn { get; set; }
        public DbSet<PortfolioItem> PortfolioItem { get; set; }
        public DbSet<Sale> Sales { get; set; }


    }
}
