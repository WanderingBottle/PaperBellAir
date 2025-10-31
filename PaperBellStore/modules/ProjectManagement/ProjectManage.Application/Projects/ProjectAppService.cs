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
        CrudAppService<PbpProject,
            ProjectDto,
            Guid,
            GetProjectListDto,
            CreateUpdateProjectDto,
            CreateUpdateProjectDto>,
        IProjectAppService
    {
        private readonly IIdentityUserRepository _userRepository;
        private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

        public ProjectAppService(IRepository<PbpProject, Guid> repository,
            IIdentityUserRepository userRepository,
            IRepository<IdentityUser, Guid> identityUserRepository) : base(repository)
        {
            _userRepository = userRepository;
            _identityUserRepository = identityUserRepository;
            GetPolicyName = ProjectManagePermissions.View;
            GetListPolicyName = ProjectManagePermissions.View;
            CreatePolicyName = ProjectManagePermissions.Create;
            UpdatePolicyName = ProjectManagePermissions.Edit;
            DeletePolicyName = ProjectManagePermissions.Delete;
        }
        protected override async Task<ProjectDto> MapToGetOutputDtoAsync(PbpProject entity)
        {
            var dto = await base.MapToGetOutputDtoAsync(entity);

            if (entity.OwnerId.HasValue)
            {
                var owner = await _userRepository.GetAsync(entity.OwnerId.Value);
                dto.OwnerName = owner.UserName;
            }

            return dto;
        }

        protected override async Task<IQueryable<PbpProject>> CreateFilteredQueryAsync(GetProjectListDto input)
        {
            var queryable = await base.CreateFilteredQueryAsync(input);

            // 模糊查询：使用Filter字段匹配Name和Description
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable = queryable.Where(x =>
                    x.Name.Contains(input.Filter) ||
                    (x.Description != null && x.Description.Contains(input.Filter)));
            }

            // 精确查询：项目名称
            if (!string.IsNullOrWhiteSpace(input.Name))
            {
                queryable = queryable.Where(x => x.Name == input.Name);
            }

            // 精确查询：项目描述
            if (!string.IsNullOrWhiteSpace(input.Description))
            {
                queryable = queryable.Where(x => x.Description == input.Description);
            }

            // 精确查询：项目状态
            if (input.Status.HasValue)
            {
                queryable = queryable.Where(x => x.Status == input.Status.Value);
            }

            // 精确查询：开始日期范围
            if (input.StartDateFrom.HasValue)
            {
                queryable = queryable.Where(x => x.StartDate >= input.StartDateFrom.Value);
            }
            if (input.StartDateTo.HasValue)
            {
                queryable = queryable.Where(x => x.StartDate <= input.StartDateTo.Value);
            }

            // 精确查询：结束日期范围
            if (input.EndDateFrom.HasValue)
            {
                queryable = queryable.Where(x => x.EndDate.HasValue && x.EndDate.Value >= input.EndDateFrom.Value);
            }
            if (input.EndDateTo.HasValue)
            {
                queryable = queryable.Where(x => x.EndDate.HasValue && x.EndDate.Value <= input.EndDateTo.Value);
            }

            // 精确查询：负责人名称（通过用户名称查找用户ID，然后过滤项目）
            if (!string.IsNullOrWhiteSpace(input.OwnerName))
            {
                // 使用IRepository<IdentityUser, Guid>查询用户
                var userQueryable = await _identityUserRepository.GetQueryableAsync();
                var ownerIds = userQueryable
                    .Where(u => u.UserName == input.OwnerName)
                    .Select(u => u.Id)
                    .ToList();

                if (ownerIds.Any())
                {
                    queryable = queryable.Where(x => x.OwnerId.HasValue && ownerIds.Contains(x.OwnerId.Value));
                }
                else
                {
                    // 如果没有找到匹配的用户，返回空结果
                    queryable = queryable.Where(x => false);
                }
            }

            return queryable;
        }
    }
}
