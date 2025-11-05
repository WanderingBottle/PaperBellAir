using Microsoft.AspNetCore.Authorization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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

        [Authorize(ProjectManagePermissions.View)]
        public async Task<byte[]> ExportToExcelAsync()
        {
            var workbook = new XSSFWorkbook();
            var tenantId = _currentTenant.Id;

            // 获取所有负责人数据
            var queryable = await Repository.GetQueryableAsync();
            var owners = queryable
                .Where(x => x.TenantId == tenantId)
                .ToList();

            // 创建负责人Sheet
            var ownerSheet = workbook.CreateSheet("负责人");
            var ownerHeaderRow = ownerSheet.CreateRow(0);
            ownerHeaderRow.CreateCell(0).SetCellValue("负责人编码");
            ownerHeaderRow.CreateCell(1).SetCellValue("负责人名称");
            ownerHeaderRow.CreateCell(2).SetCellValue("描述");
            ownerHeaderRow.CreateCell(3).SetCellValue("邮箱");
            ownerHeaderRow.CreateCell(4).SetCellValue("电话");
            ownerHeaderRow.CreateCell(5).SetCellValue("部门");

            var ownerRowIndex = 1;
            foreach (var owner in owners)
            {
                var row = ownerSheet.CreateRow(ownerRowIndex++);
                row.CreateCell(0).SetCellValue(owner.Code ?? "");
                row.CreateCell(1).SetCellValue(owner.Name ?? "");
                row.CreateCell(2).SetCellValue(owner.Description ?? "");
                row.CreateCell(3).SetCellValue(owner.Email ?? "");
                row.CreateCell(4).SetCellValue(owner.Phone ?? "");
                row.CreateCell(5).SetCellValue(GetDepartmentText(owner.Department));
            }

            // 自动调整列宽
            for (int i = 0; i < 6; i++)
            {
                ownerSheet.AutoSizeColumn(i);
            }

            // 转换为字节数组
            using var stream = new MemoryStream();
            workbook.Write(stream, true);
            return stream.ToArray();
        }

        [Authorize(ProjectManagePermissions.Create)]
        public async Task ImportFromExcelAsync(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);
            var workbook = new XSSFWorkbook(stream);

            var tenantId = _currentTenant.Id;

            // 导入负责人数据
            var ownerSheet = workbook.GetSheetAt(0); // 使用第一个sheet
            if (ownerSheet == null)
            {
                throw new Exception("Excel文件中未找到工作表");
            }

            var ownerQueryable = await Repository.GetQueryableAsync();

            for (int i = 1; i <= ownerSheet.LastRowNum; i++)
            {
                var row = ownerSheet.GetRow(i);
                if (row == null) continue;

                var code = GetCellValue(row.GetCell(0));
                var name = GetCellValue(row.GetCell(1));
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var description = GetCellValue(row.GetCell(2));
                var email = GetCellValue(row.GetCell(3));
                var phone = GetCellValue(row.GetCell(4));
                var departmentText = GetCellValue(row.GetCell(5));

                // 查找或创建负责人
                PbpOwner owner = null;

                if (!string.IsNullOrWhiteSpace(code))
                {
                    owner = ownerQueryable.FirstOrDefault(x => x.TenantId == tenantId && x.Code == code);
                }

                if (owner == null)
                {
                    // 创建新负责人
                    var createDto = new CreateUpdateOwnerDto
                    {
                        Name = name,
                        Description = description,
                        Email = email,
                        Phone = phone,
                        Department = ParseDepartment(departmentText)
                    };

                    owner = await MapToEntityAsync(createDto);
                    owner.Code = string.IsNullOrWhiteSpace(code) ? await GenerateUniqueCodeAsync() : code;
                    owner.TenantId = tenantId;
                    await Repository.InsertAsync(owner);
                }
                else
                {
                    // 更新现有负责人
                    owner.Name = name;
                    owner.Description = description;
                    owner.Email = email;
                    owner.Phone = phone;
                    owner.Department = ParseDepartment(departmentText);
                    if (string.IsNullOrWhiteSpace(owner.Code) && !string.IsNullOrWhiteSpace(code))
                    {
                        owner.Code = code;
                    }
                    await Repository.UpdateAsync(owner);
                }
            }
        }

        private string GetCellValue(ICell cell)
        {
            if (cell == null) return "";

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.StringCellValue,
                _ => ""
            };
        }

        private OwnerDepartment ParseDepartment(string departmentText)
        {
            if (string.IsNullOrWhiteSpace(departmentText))
                return OwnerDepartment.ResearchAndDevelopment;

            return departmentText switch
            {
                "研发部" => OwnerDepartment.ResearchAndDevelopment,
                "实施部" => OwnerDepartment.Implementation,
                "项目部" => OwnerDepartment.Project,
                _ => Enum.TryParse<OwnerDepartment>(departmentText, out var dept) ? dept : OwnerDepartment.ResearchAndDevelopment
            };
        }

        private string GetDepartmentText(OwnerDepartment department)
        {
            return department switch
            {
                OwnerDepartment.ResearchAndDevelopment => "研发部",
                OwnerDepartment.Implementation => "实施部",
                OwnerDepartment.Project => "项目部",
                _ => department.ToString()
            };
        }

        // 使用Default权限，因为类级别已经授权，方法级别只需要确保有权限即可
        // 下载模板只需要查看权限，但为了确保用户能访问页面，使用Default权限
        public async Task<byte[]> ExportTemplateAsync()
        {
            var workbook = new XSSFWorkbook();

            // 创建负责人Sheet（仅表头）
            var ownerSheet = workbook.CreateSheet("负责人");
            var ownerHeaderRow = ownerSheet.CreateRow(0);
            ownerHeaderRow.CreateCell(0).SetCellValue("负责人编码");
            ownerHeaderRow.CreateCell(1).SetCellValue("负责人名称");
            ownerHeaderRow.CreateCell(2).SetCellValue("描述");
            ownerHeaderRow.CreateCell(3).SetCellValue("邮箱");
            ownerHeaderRow.CreateCell(4).SetCellValue("电话");
            ownerHeaderRow.CreateCell(5).SetCellValue("部门");

            // 添加说明行
            var ownerNoteRow = ownerSheet.CreateRow(1);
            ownerNoteRow.CreateCell(0).SetCellValue("说明：");
            var ownerNoteCell = ownerNoteRow.CreateCell(1);
            ownerNoteCell.SetCellValue("1. 负责人名称为必填项；2. 负责人编码可为空（系统自动生成）；3. 部门可选值：研发部、实施部、项目部");

            // 合并说明单元格
            var ownerRegion = new NPOI.SS.Util.CellRangeAddress(1, 1, 1, 5);
            ownerSheet.AddMergedRegion(ownerRegion);

            // 自动调整列宽
            for (int i = 0; i < 6; i++)
            {
                ownerSheet.AutoSizeColumn(i);
            }

            // 转换为字节数组
            using var stream = new MemoryStream();
            workbook.Write(stream, true);
            await Task.CompletedTask;
            return stream.ToArray();
        }
    }
}

