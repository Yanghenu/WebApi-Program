using CAP;
using Microsoft.OpenApi.Models;
using Redis.Service;
using Serilog;
using SignalR;
using FusionProgram.Extensions;
using CustomConfigExtensions;
using AgileConfig.Client;
using DapperSQL;
using QuartzExtensions;
using Microsoft.Extensions.DependencyInjection;
using EFCoreLibrary;
using Orleans;
using Orleans.Hosting;
using Orleans.Storage;
using Orleans.Runtime;
using static DotNetCore.CAP.Dashboard.WarpResult;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 添加AgileConfig
builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    IConfigurationRoot configRoot = config.Build();
    if (configRoot.GetSection("AgileConfig").Exists())
    {
        config.AddAgileConfigCanReadTemplate(new ConfigClient(configRoot));
    }
});
builder.Host.ConfigureServices(async (hostingContext, services) =>
{
    // 初始化定时任务
    // 方式一
    //FusionProgram.Quartz.QuartzInit.InitJob();

    //方式二
    //services.AddQuartService();
    //services.AddServiceByInterface(o => o.Name == "FusionProgram");
    //IServiceProvider serviceProvider = services.BuildServiceProvider();
    //serviceProvider.UseQuartz(x => x.Name == "FusionProgram");

    //MinIO需要注入HttpClient
    services.AddHttpClient();
});

builder.Host.StartSiloAsync((_ctx, _builder) =>
{
    _builder.UseAdoNetClustering(_options =>
    {
        _options.Invariant = "Npgsql"; // 使用 SQL Server 的 ADO.NET 提供程序
        _options.ConnectionString = _ctx.Configuration.GetValue<string>("DataBase_ConnectionString"); // SQL Server 的连接字符串
    })
    //.ConfigureServices(svc => {
    //    svc.AddSingletonNamedService("SvcStatusStorage", (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
    //})
        .ConfigureLogging((ctx, l) => {
            if (ctx.HostingEnvironment.IsDevelopment())
            {
                l.AddConsole();
            }
        });
});

builder.Services.AddControllers();

builder.Services.AddJwtConfig(builder.Configuration);
builder.Services.AddPolicies();

// 注入EF CORE
builder.Services.AddMyEfCoreLibrary(builder.Configuration.GetValue<string>("ConnectionStrings"));

// 读取配置项
//var swaggerPort = builder.Configuration.GetValue<int>("SwaggerPort");
//var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    //配置Swagger
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "v1:接口文档",
        Description = $"API版本v1",
        Version = "v1",
        Contact = new OpenApiContact
        {
            Name = "FusionProgram",
            Email = string.Empty,
            Url = null,
        }
    });
    //配置展示注释
    {
        var path = Path.Combine(AppContext.BaseDirectory, "FusionProgram.xml");  // xml文档绝对路径
        c.IncludeXmlComments(path, true); // true : 显示控制器层注释
        c.OrderActionsBy(o => o.RelativePath); // 对action的名称进行排序，如果有多个，就可以看见效果了。
    }
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer 12345abcde",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
    //c.OperationFilter<AddParameterHeader>();
});

// 添加CAP服务的初始化
builder.Services.AddDbContext<CAP.ApplicationDbContext>(options => builder.Configuration.GetValue<string>("CAP:RedisConnectionString"));
builder.Services.AddCAP(builder.Configuration);
builder.Services.AddScoped<Publisher>();
builder.Services.AddTransient<MyMessageHandler>();

// 添加redis
builder.Services.AddSingleton<IRedisServer, RedisServer>();

// 初始化Dapper
DapperHelper.Initialize(builder.Configuration);

builder.Services.AddLogging(builder =>
{
    builder.AddFile();
});
// logger
builder.Services.AddLogging(x => {
    Log.Logger = new LoggerConfiguration()
        //.MinimumLevel.Debug()
        //.Enrich.FromLogContext()
        //.WriteTo.Console(new JsonFormatter())//控制台日志 
        .WriteTo.File($"Logs/{DateTime.Now:yyyy-MM-dd}.log")//文件日志 
        //.WriteTo.Exceptionless()//Exceptionless分布式日志 
        .CreateLogger();
    x.AddSerilog();
});

// 注册 IConfiguration
builder.Services.AddSingleton(builder.Configuration);

// 跨域配置
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => builder
                    .SetIsOriginAllowed((host) => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithOrigins("http://192.168.8.110:5600", "https://192.168.8.110:5101", "https://192.168.8.110:8090")); 
});

// 添加SignalR
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = true;
    opts.ClientTimeoutInterval = TimeSpan.FromMinutes(3000); // 设置客户端超时时间
    opts.KeepAliveInterval = TimeSpan.FromSeconds(600000000); // 设置保持连接的间隔
});

var app = builder.Build();

app.Use((context, next) =>
{
    // Log request details
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

    return next();
});

app.UseHttpsRedirection();

app.UseRouting();

// 添加跨域配置
//跨域配置要添加在userouting与useendpoints中间
//app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseCors();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    // 分别注入中间件和ui中间件
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<MyHub>("/myHub");
});

app.MapControllers();

app.Run();

