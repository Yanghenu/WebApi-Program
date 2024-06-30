using AgileConfig.Client;
using CustomConfigExtensions;
using JWT_Authentication.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace FusionProgram.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static AuthenticationBuilder AddJwtConfig(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])),
                    ClockSkew = TimeSpan.Zero,
                };
                // 允许只传token，不需要传头"Bearer token"
                if (true)
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                            return Task.CompletedTask;
                        }
                    };
                }
            });
        }

        public static IServiceCollection AddPolicies(this IServiceCollection services)
        {
            return services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Policies.Admin, Policies.AdminPolicy());
                opt.AddPolicy(Policies.User, Policies.UserPolicy());
            });
        }

        public static IHostBuilder UseCanReadTemplateAgileConfig(this IHostBuilder builder, Action<ConfigReloadedArgs> evt = null)
        {
            builder.ConfigureAppConfiguration(delegate (HostBuilderContext _, IConfigurationBuilder cfb)
            {
                // 将 AgileConfigCanReadTemplateJsonSource 添加到配置中
                cfb.AddAgileConfigCanReadTemplate(evt);
            }).ConfigureServices(delegate (HostBuilderContext ctx, IServiceCollection services)
            {
                // 向服务中添加 AgileConfig 客户端
                services.AddAgileConfig();
            });

            return builder;
        }

        /// <summary>
        /// 注入数据
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddServiceByInterface(this IServiceCollection services, Func<RuntimeLibrary, bool> loadWhere)
        {
            #region 依赖注入

            var transientType = typeof(IDenpendency); //每次新建
            var singletonType = typeof(IDenpendencySingleton); //全局唯一
            var scopedType = typeof(IDenpendencyScoped);
            if (loadWhere == null) loadWhere = o => o.Name == "FusionProgram";
            Assembly[] asss = DependencyContext.Default.RuntimeLibraries
                .Where(loadWhere)?
                .Select(o => Assembly.Load(new AssemblyName(o.Name))).ToArray();

            if (null == asss || asss.Length == 0) return services;
            List<Type> allTypes = asss.SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(transientType) || t.GetInterfaces().Contains(singletonType) || t.GetInterfaces().Contains(scopedType))).ToList();

            //class的程序集
            var implementTypes = allTypes.Where(x => x.IsClass && !x.IsAbstract).ToArray();
            //接口的程序集
            var interfaceTypes = allTypes.Where(x => x.IsInterface).ToArray();
            string dbSufixx = "";
            bool dbingoreEnable = false;
            using (var scope = services.BuildServiceProvider().CreateScope())
            {
                IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                string ig = config.GetSection("DIDBIngoreEnable")?.Value;
                if (!string.IsNullOrEmpty(ig))
                {
                    dbingoreEnable = Convert.ToBoolean(ig);
                }
                dbSufixx = config.GetSection("DBType")?.Value;
                if (string.IsNullOrEmpty(dbSufixx))
                {
                    dbSufixx = "PostgreSQL";
                }
            }
            foreach (var implementType in implementTypes)
            {
                //先反射出当前类所有实现的有效接口(接口必须继承自三种自定义生命周期接口之一)
                var assignables = interfaceTypes.Where(x => x.IsAssignableFrom(implementType)).ToArray();
                if (!assignables.Any()) //类没有实现任何自定义有效接口
                {
                    //有可能其实现的接口是一个泛型模板接口，那么要检测的方式就要多一步骤
                    //先找出该实现类的首个实现的泛型接口
                    var interfaceType = FirstGenericType(implementType);
                    //HACK 其实感觉这一步逻辑是多余的,因为FirstGenericType返回的一定是一个抽象的接口类型,
                    //这个抽象的接口类型怎么会是三个自定义生命周期接口之一呢?
                    if (interfaceType != null)
                    {
                        //剔除掉用于辅助注入的接口
                        if (interfaceType == transientType || interfaceType == scopedType || interfaceType == singletonType)
                            interfaceType = null;
                    }
                    DIHandler(interfaceType, implementType);
                }
                else
                    foreach (var interfaceType in assignables) DIHandler(interfaceType, implementType);
            }

            /* 该方法对指定类类型implementType与反射找到的实现接口类型interfaceType进行以下处理:
             * 若找到有效的自定义接口(interfaceType is null),则以接口注入的形式进行注入;
             * 否则,则以类注入的方式进行注入.
             */
            void DIHandler(Type interfaceType, Type implementType)
            {
                Type[] itsInterfaceTypes;
                //class有接口，用接口注入
                if (interfaceType != null)
                {
                    DIIngoreAttribute ingore = interfaceType.GetCustomAttribute<DIIngoreAttribute>();
                    if (null != ingore)
                    {
                        //当前接口需要判断是否启用只注入指定库的业务类
                        if (dbingoreEnable && !implementType.FullName.EndsWith(dbSufixx))
                        {
                            //当前实现的业务类没有以指定库结尾，则不将其注入
                            return;
                        }
                        else if (!dbingoreEnable && ingore.Default != implementType.FullName) return;
                    }
                    //去除重复注入
                    if (services.Any(sd => sd.ServiceType == interfaceType && sd.ImplementationType == implementType)) return;

                    itsInterfaceTypes = interfaceType.GetInterfaces();
                    //判断用什么方式注入
                    if (itsInterfaceTypes.Contains(transientType))
                        services.AddTransient(interfaceType, implementType);
                    else if (itsInterfaceTypes.Contains(singletonType))
                        services.AddSingleton(interfaceType, implementType);
                    else if (itsInterfaceTypes.Contains(scopedType))
                        services.AddScoped(interfaceType, implementType);
                }
                else //class没有接口，直接注入class
                {
                    //去除重复注入
                    if (services.Any(sd => sd.ServiceType == implementType && sd.ImplementationType == implementType)) return;
                    itsInterfaceTypes = implementType.GetInterfaces();
                    //判断用什么方式注入
                    if (itsInterfaceTypes.Contains(transientType))
                        services.AddTransient(implementType);
                    else if (itsInterfaceTypes.Contains(singletonType))
                        services.AddSingleton(implementType);
                    else if (itsInterfaceTypes.Contains(scopedType))
                        services.AddScoped(implementType);
                }
            }

            #endregion 依赖注入

            return services;
        }

        /// <summary>
        /// 提取其首个泛型接口作为注入的点
        /// </summary>
        /// <param name="givenType">给定类型</param>
        /// <returns></returns>
        public static Type FirstGenericType(Type givenType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType)
                    return it;
            }
            return null;
        }

        /// <summary>
        /// Transient Serivce
        /// </summary>
        public interface IDenpendency
        { }

        /// <summary>
        /// Singleton Serivce
        /// </summary>
        public interface IDenpendencySingleton
        { }

        /// <summary>
        /// Scoped Service
        /// </summary>
        public interface IDenpendencyScoped
        { }

        public class DIIngoreAttribute : Attribute
        {
            /// <summary>
            /// 如果忽略配置为false，则默认加载指定类型（fullname）
            /// </summary>
            public string Default { get; set; }
        }
    }
}
