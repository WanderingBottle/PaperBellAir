using Volo.Abp.Domain.Repositories;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:创建仓储接口
    /// CreateTime: 2025/10/30 13:30:16
    /// Author: Tang
    /// </summary>
    public interface IProjectRepository : IRepository<PbpProject , Guid>
    {
        Task<List<PbpProject>> GetListAsync(
                string sorting = null ,
                int maxResultCount = int.MaxValue ,
                int skipCount = 0 ,
                string filter = null ,
                CancellationToken cancellationToken = default);

        Task<long> GetCountAsync(
            string filter = null ,
            CancellationToken cancellationToken = default);
    }
}
