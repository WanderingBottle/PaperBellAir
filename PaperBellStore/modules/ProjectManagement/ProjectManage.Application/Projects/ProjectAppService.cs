using Microsoft.AspNetCore.Authorization;

using ProjectManage.Permissions;

using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

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
        private readonly ICurrentTenant _currentTenant;

        public ProjectAppService(IRepository<PbpProject, Guid> repository,
            IIdentityUserRepository userRepository,
            IRepository<IdentityUser, Guid> identityUserRepository,
            ICurrentTenant currentTenant) : base(repository)
        {
            _userRepository = userRepository;
            _identityUserRepository = identityUserRepository;
            _currentTenant = currentTenant;
            GetPolicyName = ProjectManagePermissions.View;
            GetListPolicyName = ProjectManagePermissions.View;
            CreatePolicyName = ProjectManagePermissions.Create;
            UpdatePolicyName = ProjectManagePermissions.Edit;
            DeletePolicyName = ProjectManagePermissions.Delete;
        }

        public override async Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input)
        {
            var entity = await MapToEntityAsync(input);

            // 生成唯一编码
            entity.Code = await GenerateUniqueCodeAsync();

            await Repository.InsertAsync(entity);
            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 生成唯一的项目编码
        /// 格式：PROJ + 6位数字（如 PROJ000001）
        /// </summary>
        private async Task<string> GenerateUniqueCodeAsync()
        {
            var prefix = "PROJ";
            var maxAttempts = 100;
            var attempt = 0;

            while (attempt < maxAttempts)
            {
                // 获取当前租户ID
                var tenantId = _currentTenant.Id;

                // 查询当前租户下最大的编码数字
                var queryable = await Repository.GetQueryableAsync();
                var existingCodes = queryable
                    .Where(x => x.TenantId == tenantId && x.Code != null && x.Code.StartsWith(prefix))
                    .Select(x => x.Code)
                    .ToList();

                int maxNumber = 0;
                if (existingCodes.Any())
                {
                    foreach (var code in existingCodes)
                    {
                        if (code.Length > prefix.Length)
                        {
                            var numberPart = code.Substring(prefix.Length);
                            if (int.TryParse(numberPart, out int number))
                            {
                                maxNumber = Math.Max(maxNumber, number);
                            }
                        }
                    }
                }

                // 生成新编码
                var newNumber = maxNumber + 1;
                var newCode = $"{prefix}{newNumber:D6}";

                // 检查是否已存在（防止并发问题）
                var exists = queryable.Any(x => x.TenantId == tenantId && x.Code == newCode);
                if (!exists)
                {
                    return newCode;
                }

                attempt++;
            }

            // 如果100次尝试都失败，使用时间戳作为后缀
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}{timestamp}";
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

            // 模糊查询：使用Filter字段匹配Code、Name和Description
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable = queryable.Where(x =>
                    (x.Code != null && x.Code.Contains(input.Filter)) ||
                    x.Name.Contains(input.Filter) ||
                    (x.Description != null && x.Description.Contains(input.Filter)));
            }

            // 精确查询：项目编码
            if (!string.IsNullOrWhiteSpace(input.Code))
            {
                queryable = queryable.Where(x => x.Code == input.Code);
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
