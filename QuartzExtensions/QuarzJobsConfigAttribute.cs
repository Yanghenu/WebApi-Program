namespace QuartzExtensions
{
    /// <summary>
    /// 任务配置的属性标签
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class QuarzJobsConfigAttribute : Attribute
    {
        /// <summary>
        /// 任务计时类型
        /// </summary>
        public QuartzJobType jobType { get; set; } = QuartzJobType.WithIntenal;

        /// <summary>
        /// 默认分组
        /// </summary>
        public string grounpname { get; set; } = "default";
        /// <summary>
        /// cron公式 默认时间10分钟
        /// </summary>
        public string cron { get; set; } = "0 0/10 * * * ? *";

        /// <summary>
        /// 单位毫秒 默认时间10分钟
        /// </summary>
        public int timeSpan { get; set; } = 10 * 60 * 1000;
        /// <summary>
        /// 任务描述
        /// </summary>
        public string desc { get; set; }

        /// <summary>
        /// 任务状态 运行 或者停止
        /// </summary>
        public JobState jobState { get; set; } = JobState.Start;
    }
}
