using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Net;

namespace Orleans
{
    /// <summary>
    /// 提供分布式服务器的启动与停止服务的静态类。
    /// </summary>
    public static class DistributeServer
    {
        private static ISiloHost _host;

        /// <summary>
        /// 异步启动Orleans Silo。
        /// </summary>
        /// <param name="host">IHostBuilder 用于配置和构建应用的主机。</param>
        /// <param name="builder">用于配置Silo的回调函数。</param>
        /// <param name="clusterid">集群ID，如果为null，则从配置中读取。</param>
        /// <param name="serviceid">服务ID，如果为null，则从配置中读取。</param>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>一个表示异步操作的任务。</returns>
        public static async Task StartSiloAsync(this IHostBuilder host,
            Action<Microsoft.Extensions.Hosting.HostBuilderContext, ISiloBuilder> builder = null,
            string clusterid = null,
            string serviceid = null,
            CancellationToken token = default)
        {
            // 使用Orleans启动Silo
            host.UseOrleans((_ctx, _builder) =>
            {
                // 从配置中读取探测超时、表刷新超时、和主机信息
                int probeTimeout = _ctx.Configuration.GetValue<int>("Distribute:Server:ProbeTimeout");
                int tableRefreshTimeout = _ctx.Configuration.GetValue<int>("Distribute:Server:TableRefreshTimeout");
                string hostStr = _ctx.Configuration.GetValue<string>("Distribute:Server:Host");
                string[] endpoints = hostStr.Split(",");

                // 配置集群选项
                _builder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = string.IsNullOrEmpty(clusterid) ? _ctx.Configuration.GetValue<string>("Distribute:ClusterId") : clusterid;
                    options.ServiceId = string.IsNullOrEmpty(serviceid) ? _ctx.Configuration.GetValue<string>("Distribute:ServiceId") : serviceid;
                })
                .Configure<ClusterMembershipOptions>(options =>
                {
                    // 设置集群成员选项，控制探测和死节点投票等行为
                    options.ProbeTimeout = probeTimeout == 0 ? TimeSpan.FromSeconds(3) : TimeSpan.FromSeconds(probeTimeout);
                    options.TableRefreshTimeout = tableRefreshTimeout == 0 ? TimeSpan.FromSeconds(6) : TimeSpan.FromSeconds(tableRefreshTimeout);
                    options.NumProbedSilos = 1;
                    options.NumVotesForDeathDeclaration = 1;
                    options.NumMissedProbesLimit = 2;
                    options.MaxJoinAttemptTime = TimeSpan.FromSeconds(10);
                    options.IAmAliveTablePublishTimeout = TimeSpan.FromSeconds(20);
                })
                .Configure<ConnectionOptions>(options =>
                {
                    // 配置连接选项
                    options.OpenConnectionTimeout = TimeSpan.FromSeconds(2);
                })
                .ConfigureApplicationParts(app =>
                    // 配置应用程序部件，允许Silo从应用程序基础目录加载程序集
                    app.AddFromApplicationBaseDirectory()
                )
                .ConfigureEndpoints(IPAddress.Parse(endpoints[0]), Convert.ToInt32(endpoints[1]), Convert.ToInt32(endpoints[2]), true)
                .UseDashboard(options =>
                {
                    options.Host = "*"; // 允许从所有 IP 地址访问
                    options.Port = 8080; // 设置仪表板端口
                });

                // 如果存在自定义的Silo配置回调，则调用
                builder(_ctx, _builder);
            });
        }

        /// <summary>
        /// 通过IApplicationBuilder异步启动Orleans Silo。
        /// </summary>
        /// <param name="app">IApplicationBuilder用于配置中间件的应用程序构建器。</param>
        /// <param name="builder">用于自定义Silo的回调函数。</param>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>一个表示异步操作的任务。</returns>
        public static async Task StartSiloAsync(this IApplicationBuilder app, Action<ISiloHostBuilder> builder = null, CancellationToken token = default)
        {
            // 获取配置服务
            IConfiguration config = app.ApplicationServices.GetService<IConfiguration>();

            // 启动Silo
            await StartSiloAsync(config, builder, token);
        }

        /// <summary>
        /// 通过IConfiguration异步启动Orleans Silo。
        /// </summary>
        /// <param name="config">IConfiguration 用于读取配置。</param>
        /// <param name="builder">用于自定义Silo的回调函数。</param>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>一个表示异步操作的任务。</returns>
        public static async Task StartSiloAsync(IConfiguration config, Action<ISiloHostBuilder> builder = null, CancellationToken token = default)
        {
            // 从配置中读取服务器主机信息和端点
            string hostStr = config.GetValue<string>("Distribute:Server:Host");
            string[] endpoints = hostStr.Split(",");

            // 配置SiloHostBuilder
            var _builder = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = config.GetValue<string>("Distribute:ClusterId");
                    options.ServiceId = config.GetValue<string>("Distribute:ServiceId");
                })
                .Configure<ClusterMembershipOptions>(options =>
                {
                    // 配置集群成员选项
                    options.ProbeTimeout = TimeSpan.FromSeconds(1);
                    options.TableRefreshTimeout = TimeSpan.FromSeconds(1);
                    options.NumVotesForDeathDeclaration = 1;
                })
                .Configure<ConnectionOptions>(options =>
                {
                    // 配置连接超时
                    options.OpenConnectionTimeout = TimeSpan.FromSeconds(2);
                })
                .ConfigureApplicationParts(app =>
                    // 从应用程序基础目录加载程序集
                    app.AddFromApplicationBaseDirectory()
                )
                .ConfigureEndpoints(IPAddress.Parse(endpoints[0]), Convert.ToInt32(endpoints[1]), Convert.ToInt32(endpoints[2]), true);

            // 调用自定义的Silo配置回调
            if (builder != null)
            {
                builder(_builder);
            }

            // 构建并启动Silo
            _host = _builder.Build();
            await _host.StartAsync(token);
        }

        /// <summary>
        /// 异步停止Orleans Silo。
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>一个表示异步操作的任务。</returns>
        public static async Task StopSiloAsync(CancellationToken token = default)
        {
            // 停止Silo
            await _host?.StopAsync(token);
        }
    }
}
