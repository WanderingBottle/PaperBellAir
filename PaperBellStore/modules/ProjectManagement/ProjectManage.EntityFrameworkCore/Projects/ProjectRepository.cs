using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

using PaperBellStore.EntityFrameworkCore;

using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 14:23:57
    /// Author: Tang
    /// </summary>
    public class ProjectRepository : EfCoreRepository<PaperBellStoreDbContext , PbpProject , Guid>,
        IProjectRepository
    {
        public ProjectRepository(IDbContextProvider<PaperBellStoreDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<PbpProject>> GetListAsync(string sorting = null ,
                                                         int maxResultCount = int.MaxValue ,
                                                         int skipCount = 0 ,
                                                         string filter = null ,
                                                         CancellationToken cancellationToken = default)
        {
            var query = await GetQueryableAsync();

            if(!string.IsNullOrWhiteSpace(filter))
            {
                query=query.Where(x =>
                    x.Name.Contains(filter)||
                    (x.Description!=null&&x.Description.Contains(filter)));
            }

            query=string.IsNullOrWhiteSpace(sorting)
                ? query.OrderByDescending(x => x.CreationTime)
                : query.OrderBy(sorting);

            return await query
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetCountAsync(
            string filter = null ,
            CancellationToken cancellationToken = default)
        {
            var query = await GetQueryableAsync();

            if(!string.IsNullOrWhiteSpace(filter))
            {
                query=query.Where(x =>
                    x.Name.Contains(filter)||
                    (x.Description!=null&&x.Description.Contains(filter)));
            }

            return await query.LongCountAsync(cancellationToken);
        }
    }
}
