using Newtonsoft.Json;
using System.Reflection;
using Quartz;
using Quartz.Impl;

namespace FusionProgram.Quartz
{
    /// <summary>
    /// 调度器
    /// </summary>
    public class QuartzScheduler
    {
        private IScheduler scheduler;

        /// <summary>
        /// 构造函数
        /// </summary>
        public QuartzScheduler()
        {
            // 获取默认调度器
            scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
        }

        /// <summary>
        /// 启动调度器
        /// </summary>
        public void Start()
        {
            scheduler.Start();
        }

        /// <summary>
        /// 停止调度器
        /// </summary>
        public void Stop()
        {
            scheduler.Shutdown();
        }

        /// <summary>
        /// 调度指定类型的作业
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <param name="cronExpression"></param>
        public void ScheduleJob<T>(string jobName, string jobGroup, string cronExpression) where T : IJob
        {
            var jobType = typeof(T);

            // 创建作业细节
            var jobDetail = JobBuilder.Create<T>()
                .WithIdentity(jobName, jobGroup)
                .Build();

            // 创建触发器
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{jobName}_trigger", jobGroup)
                .WithCronSchedule(cronExpression)
                .Build();

            // 将作业和触发器添加到调度器
            scheduler.ScheduleJob(jobDetail, trigger);
        }

        /// <summary>
        /// 根据字符串类型调度作业
        /// </summary>
        /// <param name="jobType"></param>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <param name="cronExpression"></param>
        public void ScheduleJobByType(string jobType, string jobName, string jobGroup, string cronExpression)
        {
            var schedulerType = typeof(QuartzScheduler);
            var scheduleJobMethod = schedulerType.GetMethod("ScheduleJob", BindingFlags.Public | BindingFlags.Instance);

            // 构造泛型方法 ScheduleJob<T>，并指定类型参数为 jobType
            var genericScheduleJobMethod = scheduleJobMethod.MakeGenericMethod(Type.GetType(jobType));

            // 调用泛型方法 ScheduleJob<T>
            genericScheduleJobMethod.Invoke(this, new object[] { jobName, jobGroup, cronExpression });
        }

        /// <summary>
        /// 获取实现了 IJob 接口的类型列表
        /// </summary>
        /// <returns></returns>
        public List<Type> GetJobTypes()
        {
            var jobTypes = new List<Type>();

            // 获取当前程序集中所有的类型
            var assembly = Assembly.GetExecutingAssembly();
            // 获取启动项目的程序集
            //var assembly = Assembly.GetEntryAssembly();
            var types = assembly.GetTypes();

            // 过滤出实现了 IJob 接口的类型
            var jobInterfaceType = typeof(IJob);
            jobTypes.AddRange(types.Where(t => jobInterfaceType.IsAssignableFrom(t)));

            return jobTypes;
        }

        /// <summary>
        /// 生成作业配置文件并返回配置信息列表
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public List<QuarzJobsConfigAttribute> GenerateJobConfigFile(string filePath)
        {
            var jobConfig = new List<QuarzJobsConfigAttribute>();

            var jobTypes = GetJobTypes();
            foreach (var jobType in jobTypes)
            {
                var attributes = jobType.GetCustomAttributes(typeof(QuarzJobsConfigAttribute), true);
                if (attributes.Length > 0)
                {
                    var configAttribute = (QuarzJobsConfigAttribute)attributes[0];
                    var jobConfigItem = new QuarzJobsConfigAttribute
                    {
                        FullName = jobType.FullName,
                        GroupName = configAttribute.GroupName,
                        Cron = configAttribute.Cron,
                        Description = configAttribute.Description,
                        JobType = configAttribute.JobType,
                        JobState = configAttribute.JobState
                    };

                    jobConfig.Add(jobConfigItem);
                }
            }

            if (File.Exists(filePath))
            {
                // 加载文件中的 job 配置项
                var list = LoadJobConfig(filePath);
                // 程序集中的 job 和文件中的 job 对比
                foreach (var item in jobConfig)
                {
                    if (list.Where(o => o.GroupName == item.GroupName).Count() == 0)
                    {
                        // 缺少的 job 添加到文件
                        list.Add(item);
                    }
                }
                // 将 jobConfig 对象保存为 JSON 配置文件
                var json = JsonConvert.SerializeObject(list, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);
                return list;
            }
            else
            {
                // 将 jobConfig 对象保存为 JSON 配置文件
                var json = JsonConvert.SerializeObject(jobConfig, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);
                return LoadJobConfig(filePath);
            }
        }

        /// <summary>
        /// 从文件加载配置项
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <returns>配置项列表</returns>
        static List<QuarzJobsConfigAttribute> LoadJobConfig(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var jobConfig = JsonConvert.DeserializeObject<List<QuarzJobsConfigAttribute>>(json);
            return jobConfig;
        }
    }
}
