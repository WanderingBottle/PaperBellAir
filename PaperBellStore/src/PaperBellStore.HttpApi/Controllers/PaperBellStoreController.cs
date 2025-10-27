using PaperBellStore.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace PaperBellStore.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class PaperBellStoreController : AbpControllerBase
{
    protected PaperBellStoreController()
    {
        LocalizationResource = typeof(PaperBellStoreResource);
    }
}
