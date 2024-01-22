using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CAP
{
    public class ApplicationDbContext : DbContext
    {
        IConfiguration _configuration;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : base(options) 
        {
            _configuration = configuration;
        }

        // 添加实体集合

        public DbSet<MessageEntity> Messages { get; set; }
        public class MyMessage
        {
            public string Text { get; set; }
        }

        public class MessageEntity
        {
            public int Id { get; set; }
            public string Content { get; set; }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 数据库连接字符串和数据库提供程序
                optionsBuilder.UseSqlServer(_configuration["CAP:SqlServerConnectionString"]);
            }
        }
    }
}
