using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CAP
{
    public static class CAPInit
    {
        public static void AddCAP(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCap(options =>
            {
                options.UseSqlServer(configuration.GetValue<string>("ConnectionStrings:DefaultConnection"));
                options.UseRedis(configuration.GetValue<string>("CAP:RedisConnectionString"));
                options.UseDashboard();
            });
        }
    }
}
