using AutoMapper;

using ProjectManage.Projects;

namespace ProjectManage
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 14:19:29
    /// Author: Tang
    /// </summary>
    public class ProjectManageApplicationAutoMapperProfile : Profile
    {
        public ProjectManageApplicationAutoMapperProfile()
        {
            CreateMap<PbpProject , ProjectDto>();
            CreateMap<CreateUpdateProjectDto , PbpProject>();
        }
    }
}
