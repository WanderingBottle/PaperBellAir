using Microsoft.AspNetCore.Authorization;

using ProjectManage.Permissions;

using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/11/4 14:14:27
    /// Author: Tang
    /// </summary>
    [Authorize(ProjectManagePermissions.Default)]
    public class OwnerAppService :
        CrudAppService<PbpOwner,
            OwnerDto,
            Guid,
            GetOwnerListDto,
            CreateUpdateOwnerDto,
            CreateUpdateOwnerDto>,
        IOwnerAppService
    {
        public OwnerAppService(IRepository<PbpOwner, Guid> repository)
            : base(repository)
        {
            GetPolicyName = ProjectManagePermissions.View;
            GetListPolicyName = ProjectManagePermissions.View;
            CreatePolicyName = ProjectManagePermissions.Create;
            UpdatePolicyName = ProjectManagePermissions.Edit;
            DeletePolicyName = ProjectManagePermissions.Delete;
        }

        protected override async Task<IQueryable<PbpOwner>> CreateFilteredQueryAsync(GetOwnerListDto input)
        {
            var queryable = await base.CreateFilteredQueryAsync(input);

            // 模糊查询：使用Filter字段匹配Name和Description
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable = queryable.Where(x =>
                    x.Name.Contains(input.Filter) ||
                    (x.Description != null && x.Description.Contains(input.Filter)) ||
                    (x.Email != null && x.Email.Contains(input.Filter)) ||
                    (x.Phone != null && x.Phone.Contains(input.Filter)));
            }

            // 精确查询：负责人名称
            if (!string.IsNullOrWhiteSpace(input.Name))
            {
                queryable = queryable.Where(x => x.Name == input.Name);
            }

            // 精确查询：负责人描述
            if (!string.IsNullOrWhiteSpace(input.Description))
            {
                queryable = queryable.Where(x => x.Description == input.Description);
            }

            // 精确查询：邮箱
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                queryable = queryable.Where(x => x.Email == input.Email);
            }

            // 精确查询：电话
            if (!string.IsNullOrWhiteSpace(input.Phone))
            {
                queryable = queryable.Where(x => x.Phone == input.Phone);
            }

            // 精确查询：部门
            if (input.Department.HasValue)
            {
                queryable = queryable.Where(x => x.Department == input.Department.Value);
            }

            return queryable;
        }
    }
}

