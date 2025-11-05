using Volo.Abp.Application.Services;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:创建应用服务接口
    /// CreateTime: 2025/10/30 13:49:27
    /// Author: Tang
    /// </summary>
    public interface IProjectAppService :
        ICrudAppService<ProjectDto,
                        Guid,
                        GetProjectListDto,
                        CreateUpdateProjectDto,
                        CreateUpdateProjectDto>
    {
        /// <summary>
        /// 导出项目数据到Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync();

        /// <summary>
        /// 从Excel导入项目数据
        /// </summary>
        Task ImportFromExcelAsync(byte[] fileContent);

        /// <summary>
        /// 导出项目Excel模板（仅表头，用于导入）
        /// </summary>
        Task<byte[]> ExportTemplateAsync();
    }
}
