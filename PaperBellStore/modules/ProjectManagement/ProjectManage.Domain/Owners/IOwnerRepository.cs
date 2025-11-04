using Volo.Abp.Domain.Repositories;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:创建仓储接口
    /// CreateTime: 2025/11/4 13:30:16
    /// Author: Tang
    /// </summary>
    public interface IOwnerRepository : IRepository<PbpOwner, Guid>
    {
        Task<List<PbpOwner>> GetListAsync(
                string sorting = null,
                int maxResultCount = int.MaxValue,
                int skipCount = 0,
                string filter = null,
                CancellationToken cancellationToken = default);

        Task<long> GetCountAsync(
            string filter = null,
            CancellationToken cancellationToken = default);
    }
}

