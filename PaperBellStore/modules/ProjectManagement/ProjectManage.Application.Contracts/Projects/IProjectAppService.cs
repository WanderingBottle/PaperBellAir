using Volo.Abp.Application.Services;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:创建应用服务接口
    /// CreateTime: 2025/10/30 13:49:27
    /// Author: Tang
    /// </summary>
    public interface IProjectAppService :
        ICrudAppService<ProjectDto ,
                        Guid ,
                        GetProjectListDto ,
                        CreateUpdateProjectDto ,
                        CreateUpdateProjectDto>
    {

    }
}
