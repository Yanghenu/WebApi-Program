namespace FusionProgram.Quartz
{
    /// <summary>
    /// QuartzInit
    /// </summary>
    public class QuartzInit
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public static void InitJob()
        {
            try
            {
                var scheduler = new QuartzScheduler();
                scheduler.Start();

                var filePath = "jobConfig.json";

                // 加载 JSON 配置文件
                var loadedJobConfig = scheduler.GenerateJobConfigFile(filePath);

                foreach (var item in loadedJobConfig)
                {
                    if (item.JobState == JobState.Stop)
                    {
                        continue;
                    }
                    // 使用加载的配置调度作业
                    scheduler.ScheduleJobByType(item.FullName, item.GroupName, item.JobGroup, item.Cron);
                }

                // 停止调度器时取消注释下面的代码
                // scheduler.Stop();
            }
            catch (Exception ex)
            { 
                
            }
        }
    }
}
