using CAP;
using Microsoft.OpenApi.Models;
using Redis.Service;
using Serilog;
using SignalR;
using FusionProgram.Extensions;
using CustomConfigExtensions;
using AgileConfig.Client;
using FusionProgram.Quartz;
using DapperSQL;

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
builder.Host.ConfigureServices((hostingContext, services) =>
{
    // 初始化定时任务
    QuartzInit.InitJob();
});

builder.Services.AddControllers();

builder.Services.AddJwtConfig(builder.Configuration);
builder.Services.AddPolicies();

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
    /*
    c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Please enter your username and password:",
        Type = SecuritySchemeType.Http,
        In = ParameterLocation.Header,
        Scheme = "basic"
    });*/
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
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Basic",
                    Type = ReferenceType.SecurityScheme
                },
                Scheme = "oauth2",
                Name = "Basic",
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

