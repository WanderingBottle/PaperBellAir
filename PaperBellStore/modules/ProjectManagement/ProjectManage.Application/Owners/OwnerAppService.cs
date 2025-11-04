using Microsoft.AspNetCore.Authorization;

using ProjectManage.Permissions;

using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

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
        private readonly ICurrentTenant _currentTenant;

        public OwnerAppService(IRepository<PbpOwner, Guid> repository, ICurrentTenant currentTenant)
            : base(repository)
        {
            _currentTenant = currentTenant;
            GetPolicyName = ProjectManagePermissions.View;
            GetListPolicyName = ProjectManagePermissions.View;
            CreatePolicyName = ProjectManagePermissions.Create;
            UpdatePolicyName = ProjectManagePermissions.Edit;
            DeletePolicyName = ProjectManagePermissions.Delete;
        }

        public override async Task<OwnerDto> CreateAsync(CreateUpdateOwnerDto input)
        {
            var entity = await MapToEntityAsync(input);

            // 生成唯一编码
            entity.Code = await GenerateUniqueCodeAsync();

            await Repository.InsertAsync(entity);
            return await MapToGetOutputDtoAsync(entity);
        }


        /// <summary>
        /// 生成唯一的负责人编码
        /// 格式：OWNER + 6位数字（如 OWNER000001）
        /// </summary>
        private async Task<string> GenerateUniqueCodeAsync()
        {
            var prefix = "OWNER";
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

        protected override async Task<IQueryable<PbpOwner>> CreateFilteredQueryAsync(GetOwnerListDto input)
        {
            var queryable = await base.CreateFilteredQueryAsync(input);

            // 模糊查询：使用Filter字段匹配Code、Name、Description、Email和Phone
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable = queryable.Where(x =>
                    (x.Code != null && x.Code.Contains(input.Filter)) ||
                    x.Name.Contains(input.Filter) ||
                    (x.Description != null && x.Description.Contains(input.Filter)) ||
                    (x.Email != null && x.Email.Contains(input.Filter)) ||
                    (x.Phone != null && x.Phone.Contains(input.Filter)));
            }

            // 精确查询：负责人编码
            if (!string.IsNullOrWhiteSpace(input.Code))
            {
                queryable = queryable.Where(x => x.Code == input.Code);
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

