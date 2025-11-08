using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using MudBlazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using PaperBellStore.Blazor.RecurringJobs;

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
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

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

        // 添加 MudBlazor 服务
        context.Services.AddMudServices();
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
            });

            // 配置序列化器
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
        });

        // 添加 Hangfire 服务器
        context.Services.AddHangfireServer(options =>
        {
            options.ServerName = "PaperBellStore-Server";  // 服务器名称
            options.WorkerCount = Environment.ProcessorCount * 5;  // 工作线程数
            options.Queues = new[] { "default", "critical", "low" };  // 队列名称
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var env = context.GetEnvironment();
        var app = context.GetApplicationBuilder();

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
        // 示例：注册定时任务
        // 可以根据实际需求添加更多任务

        // 示例1：每天 23:30 执行
        RecurringJob.AddOrUpdate<SampleRecurringJob>(
            "sample-job-daily",
            job => job.ExecuteAsync(),
            Cron.Daily(23, 30),  // 每天 23:30 执行
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        // 示例2：每小时执行一次
        // RecurringJob.AddOrUpdate<SampleRecurringJob>(
        //     "sample-job-hourly",
        //     job => job.ExecuteAsync(),
        //     Cron.Hourly());

        // 示例3：每5分钟执行一次
        // RecurringJob.AddOrUpdate<SampleRecurringJob>(
        //     "sample-job-every-5-minutes",
        //     job => job.ExecuteAsync(),
        //     "0 */5 * * *");  // Cron 表达式

        // 示例4：带参数的任务
        // RecurringJob.AddOrUpdate<SampleRecurringJob>(
        //     "sample-job-with-parameters",
        //     job => job.ExecuteWithParametersAsync("Hello", 10),
        //     Cron.Daily());
    }
}
