using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Server.AspNetCore;
using Microsoft.Extensions.Options;
using PaperBellStore.Blazor.Components;
using PaperBellStore.Blazor.Menus;
using PaperBellStore.EntityFrameworkCore;
using PaperBellStore.Localization;
using PaperBellStore.MultiTenancy;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Components.Web;
using Volo.Abp.AspNetCore.Components.Web.Theming.Routing;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.AspNetCore.Components.Server;
using Volo.Abp.AspNetCore.Components.Server.LeptonXLiteTheme;
using Volo.Abp.AspNetCore.Components.Server.LeptonXLiteTheme.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.Identity;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using PaperBellStore.Blazor.HealthChecks;
using Volo.Abp.Identity.Blazor.Server;
using Volo.Abp.TenantManagement.Blazor.Server;
using Volo.Abp.SettingManagement.Blazor.Server;
using Volo.Abp.FeatureManagement.Blazor.Server;
using Volo.Abp.Security.Claims;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Studio.Client.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;
using PaperBellStore.Blazor.Filters;
using PaperBellStore.Blazor.Middleware;
using PaperBellStore.Blazor.Uow;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;
using Microsoft.AspNetCore.Http;
using PaperBellStore.MessageQueue;
// using PaperBellStore.Blazor.RecurringJobs; // SampleRecurringJob 已移除

namespace PaperBellStore.Blazor;

[DependsOn(
    typeof(PaperBellStoreApplicationModule),
    typeof(AbpStudioClientAspNetCoreModule),
    typeof(PaperBellStoreEntityFrameworkCoreModule),
    typeof(PaperBellStoreHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpIdentityBlazorServerModule),
    typeof(AbpTenantManagementBlazorServerModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreComponentsServerLeptonXLiteThemeModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpFeatureManagementBlazorServerModule),
    typeof(AbpSettingManagementBlazorServerModule)
   // 注意：AbpEventBusRabbitMqModule 已移除
   // 如果需要在启用 RabbitMQ 时使用，请取消下面的注释
   // typeof(AbpEventBusRabbitMqModule)
   )]
public class PaperBellStoreBlazorModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(
                typeof(PaperBellStoreResource),
                typeof(PaperBellStoreDomainModule).Assembly,
                typeof(PaperBellStoreDomainSharedModule).Assembly,
                typeof(PaperBellStoreApplicationModule).Assembly,
                typeof(PaperBellStoreApplicationContractsModule).Assembly,
                typeof(PaperBellStoreBlazorModule).Assembly
            );
        });

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("PaperBellStore");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
                serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
            });
        }

        PreConfigure<AbpAspNetCoreComponentsWebOptions>(options =>
        {
            options.IsBlazorWebApp = true;
        });

        // 注意：AbpEventBusRabbitMqModule 已从 DependsOn 中移除
        // 因此不需要在这里配置 RabbitMQ 选项
        // 如果将来需要启用 RabbitMQ，请在 DependsOn 中取消注释 AbpEventBusRabbitMqModule
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        // 注意：AbpEventBusRabbitMqModule 已从 DependsOn 中移除
        // 如果启用 RabbitMQ，模块需要通过其他方式加载
        // 由于 ABP 模块系统不支持运行时动态加载，我们需要在 DependsOn 中保留模块
        // 但通过配置来阻止其初始化连接

        // Add services to the container.
        context.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        if (!configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata"))
        {
            Configure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });

            Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            });
        }
        // 注册面包屑服务（每个组件实例一个）
        context.Services.AddScoped<Services.BreadcrumbService>();

        // 确保 IHttpContextAccessor 已注册（Blazor Server 需要）
        context.Services.AddHttpContextAccessor();

        // 替换默认的 IUnitOfWorkTransactionBehaviourProvider 为安全版本
        // 修复在 Blazor Server 模式下 HttpContext.Request 可能为 null 的问题
        context.Services.Replace(
            ServiceDescriptor.Transient<IUnitOfWorkTransactionBehaviourProvider, SafeAspNetCoreUnitOfWorkTransactionBehaviourProvider>());

        ConfigureAuthentication(context);
        ConfigureUrls(configuration);
        ConfigureBundles();
        ConfigureHealthChecks(context);
        ConfigureAutoMapper();
        ConfigureVirtualFileSystem(hostingEnvironment);
        ConfigureSwaggerServices(context.Services);
        ConfigureAutoApiControllers();
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureMenu(context);
        ConfigureHangfire(context);
        ConfigureRabbitMQ(context);
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            // MVC UI
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );

            options.ScriptBundles.Configure(
                LeptonXLiteThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-scripts.js");
                }
            );

            // Blazor UI
            options.StyleBundles.Configure(
                BlazorLeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddPaperBellStoreHealthChecks();

        // 添加 RabbitMQ 健康检查
        var configuration = context.Services.GetConfiguration();
        var messageQueueConfig = configuration.GetSection("MessageQueue");
        var rabbitMQEnabled = messageQueueConfig.GetValue<bool>("RabbitMQ:Enabled", true);

        if (rabbitMQEnabled)
        {
            var rabbitMQConfig = messageQueueConfig.GetSection("RabbitMQ");
            var hostName = rabbitMQConfig["HostName"] ?? "localhost";
            var port = rabbitMQConfig.GetValue<int>("Port", 5672);
            var userName = rabbitMQConfig["UserName"] ?? "guest";
            var password = rabbitMQConfig["Password"] ?? "guest";
            var virtualHost = rabbitMQConfig["VirtualHost"] ?? "/";

            context.Services.AddHealthChecks()
                .AddRabbitMQ(
                    rabbitConnectionString: $"amqp://{userName}:{password}@{hostName}:{port}/{virtualHost}",
                    name: "rabbitmq",
                    tags: new[] { "rabbitmq", "eventbus" });
        }
    }

    private void ConfigureVirtualFileSystem(IWebHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<PaperBellStoreDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}PaperBellStore.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<PaperBellStoreDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}PaperBellStore.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<PaperBellStoreApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}PaperBellStore.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<PaperBellStoreApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}PaperBellStore.Application"));
                options.FileSets.ReplaceEmbeddedByPhysical<PaperBellStoreBlazorModule>(hostingEnvironment.ContentRootPath);
            });
        }
    }

    private void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "PaperBellStore API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }


    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        context.Services
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();
    }

    private void ConfigureMenu(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new PaperBellStoreMenuContributor());
        });
    }


    private void ConfigureRouter(ServiceConfigurationContext context)
    {
        Configure<AbpRouterOptions>(options =>
        {
            options.AppAssembly = typeof(PaperBellStoreBlazorModule).Assembly;
        });
    }

    private void ConfigureAutoApiControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(PaperBellStoreApplicationModule).Assembly);
        });
    }

    private void ConfigureAutoMapper()
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<PaperBellStoreBlazorModule>();
        });
    }

    private void ConfigureHangfire(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var connectionString = configuration.GetConnectionString("Default");

        // 配置 Hangfire（使用推荐的方法）
        context.Services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(options =>
            {
                // 使用推荐的方法设置连接字符串
                options.UseNpgsqlConnection(connectionString);

                // ===== 清理配置（可选） =====
                // 如果不需要自定义清理配置，可以注释掉以下配置，使用默认值

                // 配置清理检查间隔（默认：1 小时）
                // Hangfire 会定期检查并清理过期的任务记录
                // options.JobExpirationCheckInterval = TimeSpan.FromHours(1);

                // 配置任务保留时间（默认：24 小时）
                // 成功任务在完成后的保留时间，超过此时间的任务会被清理
                // options.JobExpirationTimeout = TimeSpan.FromHours(24);

                // 注意：不同版本的 Hangfire.PostgreSql 可能配置选项不同
                // 如果上述属性不存在，请参考对应版本的文档
            });

            // 配置序列化器
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
        });

        // 添加 Hangfire 服务器
        context.Services.AddHangfireServer(options =>
        {
            options.ServerName = "PaperBellStore-Server";  // 服务器名称
            // Worker 数量配置说明：
            // - 如果当前没有定时任务或后台任务，可以设置为 1（最小数量）
            // - 如果有任务需要执行，建议设置为 CPU 核心数 × 2 到 × 5
            // - 不能设置为 0，否则 Hangfire Server 无法启动
            // - 当前使用中等负载配置：CPU 核心数 × 3
            options.WorkerCount = Environment.ProcessorCount * 3;  // 中等负载配置
            options.Queues = new[] { "default", "critical", "low" };  // 队列名称
        });
    }

    private void ConfigureRabbitMQ(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var messageQueueConfig = configuration.GetSection("MessageQueue");
        var rabbitMQEnabled = messageQueueConfig.GetValue<bool>("RabbitMQ:Enabled", true);

        if (rabbitMQEnabled)
        {
            // 配置 RabbitMQ 连接
            // ABP RabbitMQ 模块从配置中读取连接信息
            // 使用配置绑定方式，从 MessageQueue:RabbitMQ 读取并映射到 RabbitMQ:Connections:Default
            var rabbitMQConfig = messageQueueConfig.GetSection("RabbitMQ");

            // 从 MessageQueue:RabbitMQ 读取配置并映射到 RabbitMQ:Connections:Default
            var hostName = rabbitMQConfig["HostName"] ?? "localhost";
            var port = rabbitMQConfig.GetValue<int>("Port", 5672);
            var userName = rabbitMQConfig["UserName"] ?? "guest";
            var password = rabbitMQConfig["Password"] ?? "guest";
            var virtualHost = rabbitMQConfig["VirtualHost"] ?? "/";

            // 同时使用配置绑定，确保模块可以读取配置
            // 创建一个临时的配置字典来绑定
            var rabbitMQConnectionConfig = new Dictionary<string, string?>
            {
                ["RabbitMQ:Connections:Default:HostName"] = hostName,
                ["RabbitMQ:Connections:Default:Port"] = port.ToString(),
                ["RabbitMQ:Connections:Default:UserName"] = userName,
                ["RabbitMQ:Connections:Default:Password"] = password,
                ["RabbitMQ:Connections:Default:VirtualHost"] = virtualHost
            };

            var tempConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(rabbitMQConnectionConfig)
                .Build();

            context.Services.Configure<AbpRabbitMqEventBusOptions>(
                tempConfig.GetSection("RabbitMQ"));

            // 配置事件总线
            Configure<AbpDistributedEventBusOptions>(options =>
            {
                // 可以配置事件名称映射等
            });
        }
        else
        {
            // 当 RabbitMQ 被禁用时，不需要配置任何选项
            // 因为 AbpEventBusRabbitMqModule 已从 DependsOn 中移除，模块不会被加载
        }
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var env = context.GetEnvironment();
        var app = context.GetApplicationBuilder();
        var configuration = context.GetConfiguration();

        // 检查 RabbitMQ 是否被禁用
        var messageQueueConfig = configuration.GetSection("MessageQueue");
        var rabbitMQEnabled = messageQueueConfig.GetValue<bool>("RabbitMQ:Enabled", true);

        // 如果 RabbitMQ 被禁用，尝试停止相关的后台服务
        if (!rabbitMQEnabled)
        {
            // 注意：ABP 的 RabbitMQ 模块可能会在后台启动服务
            // 如果可能，我们可以在这里尝试停止这些服务
            // 但由于模块已经初始化，这可能无法完全阻止
        }

        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
            app.UseHsts();
        }

        app.UseCorrelationId();
        app.UseRouting();
        app.MapAbpStaticAssets();
        app.UseAbpStudioLink();
        app.UseAbpSecurityHeaders();
        app.UseAntiforgery();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        // 添加 Hangfire 操作授权中间件（在 Hangfire Dashboard 之前）
        app.UseMiddleware<HangfireOperationAuthorizationMiddleware>();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "PaperBellStore API");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        // 配置 Hangfire Dashboard
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "PaperBellStore 任务调度中心",
            Authorization = new[] { new HangfireAuthorizationFilter() },
            StatsPollingInterval = 2000,  // 统计信息轮询间隔（毫秒）
            DisplayStorageConnectionString = false,  // 不显示连接字符串
            IsReadOnlyFunc = HangfireReadOnlyFilter.IsReadOnly  // 根据权限动态控制只读模式
        });

        app.UseConfiguredEndpoints(builder =>
        {
            builder.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(builder.ServiceProvider.GetRequiredService<IOptions<AbpRouterOptions>>().Value.AdditionalAssemblies.ToArray());
        });

        // 注册定时任务
        RegisterRecurringJobs(context);
    }

    private void RegisterRecurringJobs(ApplicationInitializationContext context)
    {
        // 注册日志清理定时任务
        // 每天凌晨 2:00 执行日志清理任务
        RecurringJob.AddOrUpdate<RecurringJobs.LogCleanupRecurringJob>(
            "log-cleanup-daily",
            job => job.ExecuteAsync(),
            Cron.Daily(2, 0),  // 每天凌晨 2:00 执行
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        // 注册审计日志清理定时任务
        // 每天凌晨 3:00 清理过期审计日志
        RecurringJob.AddOrUpdate<RecurringJobs.AuditLogCleanupRecurringJob>(
            "audit-log-cleanup-daily",
            job => job.ExecuteAsync(),
            Cron.Daily(3, 0),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });
    }
}
