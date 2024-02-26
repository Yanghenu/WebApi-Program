using Quartz;
using Quartz.Spi;
using Microsoft.Extensions.Logging;


namespace QuartzExtensions
{
    public class IOCJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        public IOCJobFactory(IServiceProvider serviceProvider, ILogger<IOCJobFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            try
            {
                return _serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return null;
            }
        }
        public void ReturnJob(IJob job)
        {
            (job as IDisposable)?.Dispose();
        }
    }
}
