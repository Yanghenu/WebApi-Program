namespace FusionProgram.Quartz
{
    /// <summary>
    /// QuarzJobs配置属性
    /// </summary>
    public class QuarzJobsConfigAttribute : Attribute
    {
        public string FullName { get; set; }
        public string GroupName { get; set; }
        public string JobGroup { get; set; }
        public string Cron { get; set; }
        public QuartzJobType JobType { get; set; }
        public string Description { get; set; }
        public JobState JobState { get; set; }
    }

    /// <summary>
    /// QuartzJobType
    /// </summary>
    public enum QuartzJobType
    {
        /// <summary>
        /// 使用Cron公式
        /// </summary>
        WithCron,
        // 添加其他类型，如果有需要的话
    }

    /// <summary>
    /// JobState
    /// </summary>
    public enum JobState
    {
        Start,
        Stop,
        // 添加其他状态，如果有需要的话
    }
}
