using EFCoreLibrary.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCoreLibrary
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 其他配置
        }
    }
}
