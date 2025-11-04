using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

using PaperBellStore.EntityFrameworkCore;

using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/11/4 14:23:57
    /// Author: Tang
    /// </summary>
    public class OwnerRepository : EfCoreRepository<PaperBellStoreDbContext, PbpOwner, Guid>,
        IOwnerRepository
    {
        public OwnerRepository(IDbContextProvider<PaperBellStoreDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<PbpOwner>> GetListAsync(string sorting = null,
                                                         int maxResultCount = int.MaxValue,
                                                         int skipCount = 0,
                                                         string filter = null,
                                                         CancellationToken cancellationToken = default)
        {
            var query = await GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(x =>
                    x.Name.Contains(filter) ||
                    (x.Description != null && x.Description.Contains(filter)) ||
                    (x.Email != null && x.Email.Contains(filter)) ||
                    (x.Phone != null && x.Phone.Contains(filter)));
            }

            query = string.IsNullOrWhiteSpace(sorting)
                ? query.OrderByDescending(x => x.CreationTime)
                : query.OrderBy(sorting);

            return await query
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetCountAsync(
            string filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = await GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(x =>
                    x.Name.Contains(filter) ||
                    (x.Description != null && x.Description.Contains(filter)) ||
                    (x.Email != null && x.Email.Contains(filter)) ||
                    (x.Phone != null && x.Phone.Contains(filter)));
            }

            return await query.LongCountAsync(cancellationToken);
        }
    }
}

