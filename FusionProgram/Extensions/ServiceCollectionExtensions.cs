using AgileConfig.Client;
using CustomConfigExtensions;
using JWT_Authentication.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FusionProgram.Extensions
{
    public static class ServiceCollectionExtensions
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

    }
}
