using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace QuartzExtensions
{
    public static class QuartZJob
    {
        public static void UseQuartz(this IServiceProvider serviceProvider, int maxThread = 50, params Assembly[] assemblies)
        {
            var taskService = serviceProvider.GetService<QuartzInit>();
            //永远只执行一次的这种方法可以使用阻塞形式将异步转为同步执行
            taskService.InitJob(maxThread, assemblies).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 用过一个func来查找想要的程序集
        /// </summary>
        /// <param name="applicationBuilder"></param>
        /// <param name="load"></param>
        /// <param name="maxThread"></param>
        public static void UseQuartz(this IServiceProvider serviceProvider, Func<RuntimeLibrary, bool> load, int maxThread = 50)
        {
            var taskService = serviceProvider.GetService<QuartzInit>();
            Assembly[] asss = DependencyContext.Default.RuntimeLibraries
               .Where(load)?
               .Select(o => Assembly.Load(new AssemblyName(o.Name))).ToArray();
            UseQuartz(serviceProvider, maxThread, asss);
        }
    }
}
