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

    }
}

