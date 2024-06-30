using Quartz;
using QuartzExtensions;
using static FusionProgram.Extensions.ServiceCollectionExtension;

namespace FusionProgram.Job
{
    /// <summary>
    /// 任务
    /// </summary>
    [DisallowConcurrentExecution]//只允许单个运行
    [QuarzJobsConfig(grounpname = "ExampleJob", cron = "0/5 * * * * ?", jobType = QuartzJobType.WithCron, desc = "每5秒执行一次", jobState = JobState.Start)]
    public class ExampleJob : IDenpendency, IJob
    {
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("ExampleJob Execute!");
        }
    }
}
