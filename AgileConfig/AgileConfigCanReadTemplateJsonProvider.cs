using AgileConfig.Client;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CustomConfigExtensions
{
    // 自定义的 AgileConfig 配置提供程序，用于处理配置项中的模板替换
    public class AgileConfigCanReadTemplateJsonProvider : AgileConfigProvider
    {
        private static readonly Regex _regex = new Regex(@"\{([^}]+)\}");

        public AgileConfigCanReadTemplateJsonProvider(IConfigClient client) : base(client)
        {
        }

        // 重写 TryGet 方法，在获取配置项时进行模板替换
        public override bool TryGet(string key, out string value)
        {
            bool result = base.TryGet(key, out value);
            if (!string.IsNullOrEmpty(value))
            {
                MatchCollection matches = _regex.Matches(value);
                foreach (Match match in matches)
                {
                    string temp = match.Groups[1].Value;
                    string replaceValue = Data[temp];
                    value = value.Replace("{" + temp + "}", replaceValue);
                }
            }
            return result;
        }
    }

    // 自定义的 IConfigurationSource，用于构建 AgileConfigCanReadTemplateJsonProvider 实例
    public class AgileConfigCanReadTemplateJsonSource : IConfigurationSource
    {
        protected IConfigClient ConfigClient { get; }

        public AgileConfigCanReadTemplateJsonSource(IConfigClient client)
        {
            ConfigClient = client;
        }

        // 构建 AgileConfigCanReadTemplateJsonProvider 实例
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AgileConfigCanReadTemplateJsonProvider(ConfigClient);
        }
    }

    public static class JsonConfigurationExtensions
    {
        // 扩展方法，用于向 IConfigurationBuilder 添加 AgileConfigCanReadTemplateJsonSource
        public static IConfigurationBuilder AddAgileConfigCanReadTemplate(this IConfigurationBuilder builder, IConfigClient client, Action<ConfigReloadedArgs> evt = null)
        {
            IConfigurationBuilder result = builder.Add(new AgileConfigCanReadTemplateJsonSource(client));
            if (evt != null)
            {
                // 注册重新加载事件回调
                client.ReLoaded += evt;
            }

            return result;
        }

        // 扩展方法的重载，使用默认的 ConfigClient
        public static IConfigurationBuilder AddAgileConfigCanReadTemplate(this IConfigurationBuilder builder, Action<ConfigReloadedArgs> evt = null)
        {
            return builder.AddAgileConfigCanReadTemplate(new ConfigClient(), evt);
        }

        // 扩展方法，用于在 IHostBuilder 中配置 AgileConfig
        public static IHostBuilder UseCanReadTemplateAgileConfig(this IHostBuilder builder, Action<ConfigReloadedArgs> evt = null)
        {
            builder.ConfigureAppConfiguration(delegate (HostBuilderContext _, IConfigurationBuilder cfb)
            {
                // 添加 AgileConfigCanReadTemplateJsonSource 到配置中
                cfb.AddAgileConfigCanReadTemplate(evt);
            }).ConfigureServices(delegate (HostBuilderContext ctx, IServiceCollection services)
            {
                // 向服务中添加 AgileConfig 客户端
                services.AddAgileConfig();
            });

            return builder;
        }

        // 扩展方法的重载，使用指定的 appsettings 文件名配置 AgileConfig
        public static IHostBuilder UseCanReadTemplateAgileConfig(this IHostBuilder builder, string appsettingsFileName, Action<ConfigReloadedArgs> evt = null)
        {
            builder.ConfigureAppConfiguration(delegate (HostBuilderContext _, IConfigurationBuilder cfb)
            {
                if (string.IsNullOrEmpty(appsettingsFileName))
                {
                    // 添加 AgileConfigCanReadTemplateJsonSource 到配置中（使用默认的 ConfigClient）
                    cfb.AddAgileConfigCanReadTemplate(evt);
                }
                else
                {
                    // 添加 AgileConfigCanReadTemplateJsonSource 到配置中（使用指定的 ConfigClient）
                    cfb.AddAgileConfigCanReadTemplate(new ConfigClient(appsettingsFileName), evt);
                }
            }).ConfigureServices(delegate (HostBuilderContext ctx, IServiceCollection services)
            {
                // 向服务中添加 AgileConfig 客户端
                services.AddAgileConfig();
            });

            return builder;
        }
    }
}
