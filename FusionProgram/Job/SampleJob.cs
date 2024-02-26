using Quartz;
using FusionProgram.Quartz;

namespace FusionProgram.Job
{
    /// <summary>
    /// SampleJob
    /// </summary>
    [DisallowConcurrentExecution]
    [QuarzJobsConfig(GroupName = "SampleJob", JobGroup = "Test", Cron = "0/5 * * * * ? ", JobType = QuartzJobType.WithCron, Description = "每5秒执行一次", JobState = JobState.Start)]
    public class SampleJob : IJob
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SampleJob() { }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Execute!");
        }
    }
}
