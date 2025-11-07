using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.Uow;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Studio;

namespace PaperBellStore.EntityFrameworkCore;

[DependsOn(
    typeof(PaperBellStoreDomainModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),
    typeof(AbpFeatureManagementEntityFrameworkCoreModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(BlobStoringDatabaseEntityFrameworkCoreModule)
    )]
public class PaperBellStoreEntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        // https://www.npgsql.org/efcore/release-notes/6.0.html#opting-out-of-the-new-timestamp-mapping-logic
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        PaperBellStoreEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 配置业务数据库
        context.Services.AddAbpDbContext<PaperBellStoreDbContext>(options =>
        {
            /* Remove "includeAllEntities: true" to create
             * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        // 配置日志数据库
        context.Services.AddAbpDbContext<LogDbContext>(options =>
        {
            // 日志数据库不需要默认仓库，只用于迁移
            options.AddDefaultRepositories(includeAllEntities: false);
        });

        if (AbpStudioAnalyzeHelper.IsInAnalyzeMode)
        {
            return;
        }

        Configure<AbpDbContextOptions>(options =>
        {
            /* The main point to change your DBMS.
             * See also PaperBellStoreDbContextFactory for EF Core tooling. */

            // 业务数据库配置
            options.UseNpgsql();

            // 日志数据库配置（使用 Logs 连接字符串）
            // 注意：LogDbContext 已经通过 [ConnectionStringName("Logs")] 属性指定了连接字符串名称
            // 这里只需要配置使用 Npgsql，连接字符串会自动从配置中获取
            options.Configure<LogDbContext>(opts =>
            {
                opts.UseNpgsql();
            });
        });

        // 配置日志数据库连接字符串
        Configure<AbpDbConnectionOptions>(options =>
        {
            var connectionString = context.Services.GetConfiguration()
                .GetConnectionString("Logs");
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.ConnectionStrings["Logs"] = connectionString;
            }
        });
    }
}
