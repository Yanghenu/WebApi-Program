using System.Text.Json.Serialization;

namespace QuartzExtensions
{
    public class QuartzJobModel
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string taskName { get; set; }
        /// <summary>
        /// 分组名称
        /// </summary>
        public string groupName { get; set; }
        /// <summary>
        /// cron公式
        /// </summary>
        public string cron { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string desc { get; set; }
        /// <summary>
        /// 任务执行的时间类型
        /// </summary>
        public QuartzJobType quartzJobType { get; set; }
        /// <summary>
        /// 时间间隔
        /// </summary>
        public TimeSpan timeSpan { get; set; }
        /// <summary>
        /// jobMapData 
        /// </summary>
        public List<JobParam> param { get; set; }
        /// <summary>
        /// 任务反射用的类型
        /// </summary>
        [JsonIgnore]
        public Type type { get; set; }
        /// <summary>
        /// 任务状态
        /// </summary>
        public JobState JobState { get; set; } = QuartzExtensions.JobState.Start;
    }

    public class JobParam
    {
        public string k { get; set; }
        public object v { get; set; }
    }
}
