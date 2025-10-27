using Volo.Abp.Settings;

namespace PaperBellStore.Settings;

public class PaperBellStoreSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(PaperBellStoreSettings.MySetting1));
    }
}
