using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCoreLibrary
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMyEfCoreLibrary(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<ProductService>();
            return services;
        }
    }
}
