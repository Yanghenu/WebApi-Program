using Microsoft.Extensions.Logging;
using Quartz.Spi;
using Quartz;
using System.Reflection;
using System.Text.Json;
using System.Text.Unicode;

namespace QuartzExtensions
{
    public class QuartzInit
    {
        private readonly IJobFactory _iOCJobFactory;
        private readonly ILoggerFactory _loggerFactory;
        private IScheduler _scheduler;
        //所有作业配置存储文件
        private static string JobConfigFileName = "JobConfig.json";
        public QuartzInit(IJobFactory iOCJobFactory, ILoggerFactory loggerFactory)
        {
            _iOCJobFactory = iOCJobFactory;
            _loggerFactory = loggerFactory;
        }

        public async Task InitJob(int maxThread = 50, params Assembly[] assemblies)
        {
            var basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            string jobConfig = QuartzUtils.ReadFile(Path.Combine(basePath, JobConfigFileName));
            //通过反射获取所有任务
            var listJobs = GetAllJob(assemblies);

            //SampleJob不使用当前方式启动，排除掉
            listJobs.RemoveAll(x => x.taskName.Contains("SampleJob"));

            //如果文件不为空 读取配置
            if (!string.IsNullOrEmpty(jobConfig))
            {
                try
                {
                    var filejobs = JsonSerializer.Deserialize<List<QuartzJobModel>>(jobConfig);
                    foreach (var item in filejobs)
                    {
                        var filejob = listJobs.Where(x => x.taskName == item.taskName).FirstOrDefault();
                        if (filejob != null)
                        {
                            filejob.groupName = item.groupName;
                            filejob.cron = item.cron;
                            filejob.desc = item.desc;
                            filejob.param = item.param;
                            filejob.quartzJobType = item.quartzJobType;
                            //filejob.type = item.type;
                            filejob.JobState = item.JobState;
                            filejob.timeSpan = item.timeSpan;
                            // filejob.totalMiliseconds = item.totalMiliseconds;
                            // filejob.timeSpan = TimeSpan.FromMilliseconds(item.totalMiliseconds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _loggerFactory.CreateLogger<QuartzInit>().LogError(ex.ToString(), "JobConfig.json文件内容解析出错，将重置为默认值");
                }
            }


            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All);
            //json 美化设置 
            options.WriteIndented = true;
            ////写入文件
            QuartzUtils.WriteFile(AppDomain.CurrentDomain.BaseDirectory, JobConfigFileName,
                JsonSerializer.Serialize(listJobs, options));
            //要执行的
            var execteudicjob = new Dictionary<string, (IJobDetail, ITrigger)>();

            foreach (var item in listJobs)
            {
                if (item.JobState == JobState.Stop)
                    continue;
                if (item.quartzJobType == QuartzJobType.WithCron && QuartzUtils.IsValidExpression(item.cron).Item1)
                {
                    var jobandtriger = QuartzUtils.CreateQuartzTask(item.type, item.taskName, item.groupName, item.cron,
                        item.param != null ? item.param.ToDictionary(key => key.k, value => value.v) : null);
                    execteudicjob.Add(item.taskName, jobandtriger);
                }
                else if (item.quartzJobType == QuartzJobType.WithIntenal)
                {
                    var jobandtriger = QuartzUtils.CreateSimpleQuartzTask(item.type, item.taskName, item.groupName,
                        item.timeSpan, item.param != null ? item.param.ToDictionary(key => key.k, value => value.v) : null);
                    execteudicjob.Add(item.taskName, jobandtriger);
                }
                else
                {
                    _loggerFactory.CreateLogger<QuartzInit>().LogError(item.taskName + "启动失败:" + "cron公式不正确");
                    // Console.WriteLine(item.taskName + "启动失败:" + "cron公式不正确");
                }
            }

            _scheduler = await SchedulerBuilder.Create()
                .UseDefaultThreadPool(x => x.MaxConcurrency = maxThread)
                .BuildScheduler();


            _scheduler.JobFactory = _iOCJobFactory;
            foreach (var item in execteudicjob)
            {
                await _scheduler.ScheduleJob(item.Value.Item1, item.Value.Item2);
                await _scheduler.Start(); //启动
            }

            Quartz.Logging.LogContext.SetCurrentLogProvider(_loggerFactory);
        }

        public async Task ShutDown()
        {
            await _scheduler.Shutdown();
        }

        private List<QuartzJobModel> GetAllJob(params Assembly[] assemblies)
        {
            var listJobs = new List<QuartzJobModel>();
            List<Type> allTypes = new List<Type>();
            foreach (var item in assemblies)
            {
                allTypes.AddRange(item.GetTypes().ToList().Where(x => x.IsClass).ToList());
            }

            var ijob = typeof(IJob);
            foreach (var item in allTypes)
            {
                if (item.GetInterfaces().Contains(ijob))
                {
                    var job = new QuartzJobModel()
                    {
                        taskName = item.GetTypeInfo().FullName,
                        type = item
                    };
                    var attr = item.GetCustomAttribute(typeof(QuarzJobsConfigAttribute)) as QuarzJobsConfigAttribute;
                    if (attr != null)
                    {
                        job.groupName = attr.GetPropertyValue("grounpname").ToString();
                        job.cron = attr.GetPropertyValue("cron").ToString();
                        job.desc = attr.GetPropertyValue("desc")?.ToString();
                        job.quartzJobType = (QuartzJobType)attr.GetPropertyValue("jobType");
                        job.JobState = (JobState)attr.GetPropertyValue("jobState");
                        job.timeSpan = TimeSpan.FromMilliseconds((int)attr.GetPropertyValue("timeSpan"));
                        ;
                    }

                    listJobs.Add(job);
                }

                ;
            }

            return listJobs;
        }
    }
}
