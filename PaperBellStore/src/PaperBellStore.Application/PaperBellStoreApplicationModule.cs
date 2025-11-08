using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;
using PaperBellStore.MessageQueue;

namespace PaperBellStore;

[DependsOn(
    typeof(PaperBellStoreDomainModule),
    typeof(PaperBellStoreApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class PaperBellStoreApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        // 配置消息队列选项（RabbitMQ 配置在 Blazor 模块中完成）
        context.Services.Configure<MessageQueueOptions>(
            configuration.GetSection("MessageQueue"));

        var messageQueueConfig = configuration.GetSection("MessageQueue");

        // 获取 RabbitMQ 和 MQTT 的启用状态
        var rabbitMQEnabled = messageQueueConfig.GetValue<bool>("RabbitMQ:Enabled", true);
        var mqttEnabled = messageQueueConfig.GetValue<bool>("Mqtt:Enabled", false);

        // 条件性注册 MQTT 服务
        if (mqttEnabled)
        {
            var mqttConfig = messageQueueConfig.GetSection("Mqtt");
            var enablePublish = mqttConfig.GetValue<bool>("EnablePublish", true);
            var enableSubscribe = mqttConfig.GetValue<bool>("EnableSubscribe", true);

            if (enablePublish || enableSubscribe)
            {
                context.Services.AddSingleton<MqttService>();
                context.Services.AddHostedService<MqttService>(sp => sp.GetRequiredService<MqttService>());
            }
        }

        // 条件性注册消息桥接服务
        if (rabbitMQEnabled && mqttEnabled)
        {
            // 注册消息桥接服务（可选）
            context.Services.AddSingleton<MessageBridgeService>();
            context.Services.AddHostedService<MessageBridgeService>(sp =>
                sp.GetRequiredService<MessageBridgeService>());
        }

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<PaperBellStoreApplicationModule>();
        });
    }
}
