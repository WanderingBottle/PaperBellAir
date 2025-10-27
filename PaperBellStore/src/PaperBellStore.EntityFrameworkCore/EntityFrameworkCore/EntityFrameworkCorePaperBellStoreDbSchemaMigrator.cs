using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaperBellStore.Data;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.EntityFrameworkCore;

public class EntityFrameworkCorePaperBellStoreDbSchemaMigrator
    : IPaperBellStoreDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCorePaperBellStoreDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the PaperBellStoreDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<PaperBellStoreDbContext>()
            .Database
            .MigrateAsync();
    }
}
