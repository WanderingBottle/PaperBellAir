using Microsoft.Extensions.Localization;
using PaperBellStore.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace PaperBellStore.Blazor;

[Dependency(ReplaceServices = true)]
public class PaperBellStoreBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<PaperBellStoreResource> _localizer;

    public PaperBellStoreBrandingProvider(IStringLocalizer<PaperBellStoreResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
