using Volo.Abp.Application.Services;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:创建应用服务接口
    /// CreateTime: 2025/11/4 13:49:27
    /// Author: Tang
    /// </summary>
    public interface IOwnerAppService :
        ICrudAppService<OwnerDto,
                        Guid,
                        GetOwnerListDto,
                        CreateUpdateOwnerDto,
                        CreateUpdateOwnerDto>
    {
        /// <summary>
        /// 导出负责人数据到Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync();

        /// <summary>
        /// 从Excel导入负责人数据
        /// </summary>
        Task ImportFromExcelAsync(byte[] fileContent);

        /// <summary>
        /// 导出负责人Excel模板（仅表头，用于导入）
        /// </summary>
        Task<byte[]> ExportTemplateAsync();
    }
}

