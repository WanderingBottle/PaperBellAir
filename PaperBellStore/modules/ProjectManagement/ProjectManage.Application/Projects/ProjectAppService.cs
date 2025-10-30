using Microsoft.AspNetCore.Authorization;

using ProjectManage.Permissions;

using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 14:14:27
    /// Author: Tang
    /// </summary>
    [Authorize(ProjectManagePermissions.Default)]
    public class ProjectAppService :
        CrudAppService<PbpProject ,
            ProjectDto ,
            Guid ,
            GetProjectListDto ,
            CreateUpdateProjectDto ,
            CreateUpdateProjectDto>,
        IProjectAppService
    {
        private readonly IIdentityUserRepository _userRepository;

        public ProjectAppService(IRepository<PbpProject , Guid> repository ,
            IIdentityUserRepository userRepository) : base(repository)
        {
            _userRepository=userRepository;
            GetPolicyName=ProjectManagePermissions.View;
            GetListPolicyName=ProjectManagePermissions.View;
            CreatePolicyName=ProjectManagePermissions.Create;
            UpdatePolicyName=ProjectManagePermissions.Edit;
            DeletePolicyName=ProjectManagePermissions.Delete;
        }
        protected override async Task<ProjectDto> MapToGetOutputDtoAsync(PbpProject entity)
        {
            var dto = await base.MapToGetOutputDtoAsync(entity);

            if(entity.OwnerId.HasValue)
            {
                var owner = await _userRepository.GetAsync(entity.OwnerId.Value);
                dto.OwnerName=owner.UserName;
            }

            return dto;
        }

        protected override async Task<IQueryable<PbpProject>> CreateFilteredQueryAsync(GetProjectListDto input)
        {
            var queryable = await base.CreateFilteredQueryAsync(input);

            if(!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable=queryable.Where(x =>
                    x.Name.Contains(input.Filter)||
                    (x.Description!=null&&x.Description.Contains(input.Filter)));
            }

            if(input.Status.HasValue)
            {
                queryable=queryable.Where(x => x.Status==input.Status.Value);
            }

            return queryable;
        }
    }
}
