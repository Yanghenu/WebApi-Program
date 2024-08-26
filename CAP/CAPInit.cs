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
                options.UsePostgreSql(configuration.GetValue<string>("DataBase_ConnectionString"));
                options.UseRedis(configuration.GetValue<string>("CAP:RedisConnectionString"));
                options.UseDashboard();
            });
        }
    }
}
