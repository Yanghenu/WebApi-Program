using Microsoft.Extensions.DependencyInjection;
using Quartz.Spi;

namespace QuartzExtensions
{
    public static class QuartzSetup
    {
        public static void AddQuartService(this IServiceCollection services)
        {
            services.AddTransient<IJobFactory, IOCJobFactory>();
            services.AddTransient(typeof(QuartzInit));
        }
    }
}
