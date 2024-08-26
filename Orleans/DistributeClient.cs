using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Orleans
{
    public static class DistributeClient
    {
        /// <summary>
        /// 使用应用程序构建器连接到Orleans集群的扩展方法。
        /// </summary>
        /// <param name="app">IApplicationBuilder实例。</param>
        /// <param name="builder">可选的IClientBuilder配置操作。</param>
        /// <returns>初始化后的IClusterClient实例。</returns>
        public static async Task<IClusterClient> ConnectClientAsync(this IApplicationBuilder app, Action<IClientBuilder> builder = null)
        {
            // 从应用程序服务中获取配置服务
            IConfiguration config = app.ApplicationServices.GetService<IConfiguration>();
            return await ConnectClientAsync(config, builder);
        }

        /// <summary>
        /// 使用指定的配置连接到Orleans集群。
        /// </summary>
        /// <param name="config">配置实例（IConfiguration）。</param>
        /// <param name="builder">可选的IClientBuilder配置操作。</param>
        /// <param name="clusterid">集群ID，可选。</param>
        /// <param name="serviceid">服务ID，可选。</param>
        /// <returns>初始化后的IClusterClient实例。</returns>
        public static async Task<IClusterClient> ConnectClientAsync(IConfiguration config, Action<IClientBuilder> builder = null, string clusterid = null, string serviceid = null)
        {
            IClusterClient client;
            // 获取配置中的集群地址列表
            string[] clusters = config.GetSection("Distribute:Client:Clusters").Get<string[]>();
            IPEndPoint[] endPoints = new IPEndPoint[clusters.Length];
            for (int i = 0; i < clusters.Length; i++)
            {
                // 解析集群地址为IPEndPoint
                endPoints[i] = IPEndPoint.Parse(clusters[i]);
            }

            // 创建并配置ClientBuilder
            var _builder = new ClientBuilder()
                .UseStaticClustering(endPoints) // 使用静态集群配置
                .Configure<ClusterOptions>(options =>
                {
                    // 配置集群和服务ID
                    options.ClusterId = string.IsNullOrEmpty(clusterid) ? config.GetValue<string>("Distribute:ClusterId") : clusterid;
                    options.ServiceId = string.IsNullOrEmpty(serviceid) ? config.GetValue<string>("Distribute:ServiceId") : serviceid;
                })
                .Configure<GatewayOptions>(options => {
                    // 配置网关刷新周期
                    options.GatewayListRefreshPeriod = TimeSpan.FromSeconds(1);
                })
                .Configure<ConnectionOptions>(options => {
                    // 配置连接超时时间
                    options.OpenConnectionTimeout = TimeSpan.FromSeconds(2);
                })
                .Configure<ClientMessagingOptions>(options => {
                    // 配置响应超时时间，当silo切换时，如果在设定时间内未收到响应，则切换到另一个silo节点
                    options.ResponseTimeout = TimeSpan.FromSeconds(5);
                });

            // 如果有自定义的builder配置，执行它
            if (builder != null)
            {
                builder(_builder);
            }

            // 构建Client并连接
            client = _builder.Build();
            await client.Connect(ex => Task.FromResult(true)); // 如果连接到至少一个网关即认为连接成功

            Console.WriteLine("客户端已成功连接到silo主机\n");
            return client;
        }
    }
}
