using PaperBellStore.Localization;
using Volo.Abp.AspNetCore.Components;

namespace PaperBellStore.Blazor;

public abstract class PaperBellStoreComponentBase : AbpComponentBase
{
    protected PaperBellStoreComponentBase()
    {
        LocalizationResource = typeof(PaperBellStoreResource);
    }
}
