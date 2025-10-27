using PaperBellStore.Localization;
using Volo.Abp.Application.Services;

namespace PaperBellStore;

/* Inherit your application services from this class.
 */
public abstract class PaperBellStoreAppService : ApplicationService
{
    protected PaperBellStoreAppService()
    {
        LocalizationResource = typeof(PaperBellStoreResource);
    }
}
