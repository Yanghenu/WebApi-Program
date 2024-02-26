using Quartz.Impl.Triggers;
using Quartz;
using System.Reflection;

namespace QuartzExtensions
{
    public static class QuartzUtils
    {
        /// <summary>
        /// 根据cron 公式创造一个 定时任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="taskName"></param>
        /// <param name="groupName"></param>
        /// <param name="cron"></param>
        /// <param name="jobparam"></param>
        /// <returns></returns>
        public static (IJobDetail, ITrigger) CreateQuartzTask(Type t, string taskName, string groupName, string Interval, Dictionary<string, object> jobparam = null)
        {
            //job明细

            IJobDetail job = JobBuilder.Create(t)
                .WithIdentity(taskName, groupName)
                .Build();

            ///触发器
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(taskName, groupName)
                .WithCronSchedule(Interval)
                .Build();
            ///初始化参数
            if (jobparam != null)
            {
                foreach (var item in jobparam)
                {
                    job.JobDataMap.Add(item.Key, item.Value);
                }
            }
            return (job, trigger);
        }

        /// <summary>
        /// 根据timespan创建一个永不停止的任务
        /// </summary>
        /// <param name="t"></param>
        /// <param name="taskName"></param>
        /// <param name="groupName"></param>
        /// <param name="timeSpan"></param>
        /// <param name="jobparam"></param>
        /// <returns></returns>
        public static (IJobDetail, ITrigger) CreateSimpleQuartzTask(Type t, string taskName, string groupName, TimeSpan timeSpan, Dictionary<string, object> jobparam = null)
        {
            //job明细

            IJobDetail job = JobBuilder.Create(t)
                .WithIdentity(taskName, groupName)
                .Build();
            ///触发器
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(taskName, groupName)
                .WithSimpleSchedule(x => x.RepeatForever().WithInterval(timeSpan))
                .Build();
            ///初始化参数
            if (jobparam != null)
            {
                foreach (var item in jobparam)
                {
                    job.JobDataMap.Add(item.Key, item.Value);
                }
            }
            return (job, trigger);
        }

        /// <summary>
        /// 验证时间公式是否正常
        /// </summary>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        public static (bool, string) IsValidExpression(string cronExpression)
        {
            try
            {
                CronTriggerImpl trigger = new CronTriggerImpl();
                trigger.CronExpressionString = cronExpression;
                DateTimeOffset? date = trigger.ComputeFirstFireTimeUtc(null);
                return (date != null, date == null ? $"请确认表达式{cronExpression}是否正确!" : "");
            }
            catch (Exception e)
            {
                return (false, $"请确认表达式{cronExpression}是否正确!{e.Message}");
            }
        }
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)?.GetValue(obj);
        }

        public static string ReadFile(string path)
        {
            if (!File.Exists(path))
                return "";
            using (StreamReader stream = new StreamReader(path))
            {
                return stream.ReadToEnd(); // 读取文件
            }
        }

        public static void WriteFile(string path, string fileName, string content, bool appendToLast = false)
        {
            if (!Directory.Exists(path))//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(path);
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(path, fileName), appendToLast, System.Text.Encoding.UTF8))
            {
                sw.WriteLine(content);
                sw.Close();
                sw.Dispose();
            }
        }
    }
}
