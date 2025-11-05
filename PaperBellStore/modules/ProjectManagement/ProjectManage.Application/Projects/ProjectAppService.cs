using Microsoft.AspNetCore.Authorization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ProjectManage.Owners;
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
        private readonly IRepository<PbpOwner, Guid> _ownerRepository;
        private readonly ICurrentTenant _currentTenant;

        public ProjectAppService(IRepository<PbpProject, Guid> repository,
            IIdentityUserRepository userRepository,
            IRepository<IdentityUser, Guid> identityUserRepository,
            IRepository<PbpOwner, Guid> ownerRepository,
            ICurrentTenant currentTenant) : base(repository)
        {
            _userRepository = userRepository;
            _identityUserRepository = identityUserRepository;
            _ownerRepository = ownerRepository;
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

        [Authorize(ProjectManagePermissions.View)]
        public async Task<byte[]> ExportToExcelAsync()
        {
            var workbook = new XSSFWorkbook();
            var tenantId = _currentTenant.Id;

            // 获取所有项目数据
            var queryable = await Repository.GetQueryableAsync();
            var projects = queryable
                .Where(x => x.TenantId == tenantId)
                .ToList();

            // 获取所有Owner数据用于关联
            var ownerQueryable = await _ownerRepository.GetQueryableAsync();
            var owners = ownerQueryable
                .Where(x => x.TenantId == tenantId)
                .ToList();
            var ownerDict = owners.ToDictionary(x => x.Id, x => x);

            // 创建项目Sheet
            var projectSheet = workbook.CreateSheet("项目");
            var projectHeaderRow = projectSheet.CreateRow(0);
            projectHeaderRow.CreateCell(0).SetCellValue("项目名称");
            projectHeaderRow.CreateCell(1).SetCellValue("描述");
            projectHeaderRow.CreateCell(2).SetCellValue("开始日期");
            projectHeaderRow.CreateCell(3).SetCellValue("结束日期");
            projectHeaderRow.CreateCell(4).SetCellValue("状态");
            projectHeaderRow.CreateCell(5).SetCellValue("责任人");

            var projectRowIndex = 1;
            foreach (var project in projects)
            {
                var row = projectSheet.CreateRow(projectRowIndex++);
                row.CreateCell(0).SetCellValue(project.Name ?? "");
                row.CreateCell(1).SetCellValue(project.Description ?? "");

                var startDateCell = row.CreateCell(2);
                startDateCell.SetCellValue(project.StartDate);
                var startDateStyle = workbook.CreateCellStyle();
                var startDateFormat = workbook.CreateDataFormat();
                startDateStyle.DataFormat = startDateFormat.GetFormat("yyyy-MM-dd");
                startDateCell.CellStyle = startDateStyle;

                if (project.EndDate.HasValue)
                {
                    var endDateCell = row.CreateCell(3);
                    endDateCell.SetCellValue(project.EndDate.Value);
                    var endDateStyle = workbook.CreateCellStyle();
                    var endDateFormat = workbook.CreateDataFormat();
                    endDateStyle.DataFormat = endDateFormat.GetFormat("yyyy-MM-dd");
                    endDateCell.CellStyle = endDateStyle;
                }
                else
                {
                    row.CreateCell(3).SetCellValue("");
                }

                row.CreateCell(4).SetCellValue(GetStatusText(project.Status));
                var ownerCode = project.OwnerId.HasValue && ownerDict.ContainsKey(project.OwnerId.Value)
                    ? ownerDict[project.OwnerId.Value].Code ?? ""
                    : "";
                row.CreateCell(5).SetCellValue(ownerCode);
            }

            // 创建负责人Sheet（使用已获取的owners数据）
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
                projectSheet.AutoSizeColumn(i);
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

            // 先导入负责人数据
            var ownerSheet = workbook.GetSheet("负责人");
            if (ownerSheet == null)
            {
                throw new Exception("Excel文件中未找到'负责人'工作表");
            }

            var ownerCodeMap = new Dictionary<string, Guid>();
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
                var ownerQueryable = await _ownerRepository.GetQueryableAsync();
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

                    owner = new PbpOwner(Guid.NewGuid(), name, description);
                    owner.Email = email;
                    owner.Phone = phone;
                    owner.Department = ParseDepartment(departmentText);
                    owner.Code = string.IsNullOrWhiteSpace(code) ? await GenerateOwnerCodeAsync() : code;
                    owner.TenantId = tenantId;
                    await _ownerRepository.InsertAsync(owner);
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
                    await _ownerRepository.UpdateAsync(owner);
                }

                if (!string.IsNullOrWhiteSpace(owner.Code))
                {
                    ownerCodeMap[owner.Code] = owner.Id;
                }
            }

            // 导入项目数据
            var projectSheet = workbook.GetSheet("项目");
            if (projectSheet == null)
            {
                throw new Exception("Excel文件中未找到'项目'工作表");
            }

            for (int i = 1; i <= projectSheet.LastRowNum; i++)
            {
                var row = projectSheet.GetRow(i);
                if (row == null) continue;

                var name = GetCellValue(row.GetCell(0));
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var description = GetCellValue(row.GetCell(1));
                var startDate = GetCellValueAsDateTime(row.GetCell(2));
                var endDate = GetCellValueAsDateTime(row.GetCell(3));
                var statusText = GetCellValue(row.GetCell(4));
                var ownerCode = GetCellValue(row.GetCell(5));

                // 查找或创建项目
                var projectQueryable = await Repository.GetQueryableAsync();
                var project = projectQueryable.FirstOrDefault(x => x.TenantId == tenantId && x.Name == name);

                if (project == null)
                {
                    // 创建新项目
                    var createDto = new CreateUpdateProjectDto
                    {
                        Name = name,
                        Description = description,
                        StartDate = startDate ?? DateTime.Now,
                        EndDate = endDate,
                        Status = ParseStatus(statusText),
                        OwnerId = !string.IsNullOrWhiteSpace(ownerCode) && ownerCodeMap.ContainsKey(ownerCode)
                            ? ownerCodeMap[ownerCode]
                            : null
                    };

                    project = await MapToEntityAsync(createDto);
                    project.Code = await GenerateUniqueCodeAsync();
                    project.TenantId = tenantId;
                    await Repository.InsertAsync(project);
                }
                else
                {
                    // 更新现有项目
                    project.Description = description;
                    project.StartDate = startDate ?? project.StartDate;
                    project.EndDate = endDate;
                    project.Status = ParseStatus(statusText);
                    project.OwnerId = !string.IsNullOrWhiteSpace(ownerCode) && ownerCodeMap.ContainsKey(ownerCode)
                        ? ownerCodeMap[ownerCode]
                        : null;
                    await Repository.UpdateAsync(project);
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

        private DateTime? GetCellValueAsDateTime(ICell cell)
        {
            if (cell == null) return null;

            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
            {
                return cell.DateCellValue;
            }

            if (DateTime.TryParse(GetCellValue(cell), out var date))
            {
                return date;
            }

            return null;
        }

        private ProjectStatus ParseStatus(string statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
                return ProjectStatus.Planning;

            return statusText switch
            {
                "规划中" => ProjectStatus.Planning,
                "进行中" => ProjectStatus.InProgress,
                "暂停" => ProjectStatus.OnHold,
                "已完成" => ProjectStatus.Completed,
                "已取消" => ProjectStatus.Cancelled,
                _ => Enum.TryParse<ProjectStatus>(statusText, out var status) ? status : ProjectStatus.Planning
            };
        }

        private string GetStatusText(ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Planning => "规划中",
                ProjectStatus.InProgress => "进行中",
                ProjectStatus.OnHold => "暂停",
                ProjectStatus.Completed => "已完成",
                ProjectStatus.Cancelled => "已取消",
                _ => status.ToString()
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

        private async Task<string> GenerateOwnerCodeAsync()
        {
            var prefix = "OWNER";
            var maxAttempts = 100;
            var attempt = 0;
            var tenantId = _currentTenant.Id;

            while (attempt < maxAttempts)
            {
                var queryable = await _ownerRepository.GetQueryableAsync();
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

                var newNumber = maxNumber + 1;
                var newCode = $"{prefix}{newNumber:D6}";

                var exists = queryable.Any(x => x.TenantId == tenantId && x.Code == newCode);
                if (!exists)
                {
                    return newCode;
                }

                attempt++;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}{timestamp}";
        }

        // 使用Default权限，因为类级别已经授权，方法级别只需要确保有权限即可
        // 下载模板只需要查看权限，但为了确保用户能访问页面，使用Default权限
        public async Task<byte[]> ExportTemplateAsync()
        {
            var workbook = new XSSFWorkbook();

            // 创建项目Sheet（仅表头）
            var projectSheet = workbook.CreateSheet("项目");
            var projectHeaderRow = projectSheet.CreateRow(0);
            projectHeaderRow.CreateCell(0).SetCellValue("项目名称");
            projectHeaderRow.CreateCell(1).SetCellValue("描述");
            projectHeaderRow.CreateCell(2).SetCellValue("开始日期");
            projectHeaderRow.CreateCell(3).SetCellValue("结束日期");
            projectHeaderRow.CreateCell(4).SetCellValue("状态");
            projectHeaderRow.CreateCell(5).SetCellValue("责任人");

            // 添加说明行
            var projectNoteRow = projectSheet.CreateRow(1);
            projectNoteRow.CreateCell(0).SetCellValue("说明：");
            var noteCell = projectNoteRow.CreateCell(1);
            noteCell.SetCellValue("1. 项目名称为必填项；2. 开始日期格式：yyyy-MM-dd；3. 状态可选值：规划中、进行中、暂停、已完成、已取消；4. 责任人为负责人编码（可为空）");

            // 合并说明单元格
            var region = new NPOI.SS.Util.CellRangeAddress(1, 1, 1, 5);
            projectSheet.AddMergedRegion(region);

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
                projectSheet.AutoSizeColumn(i);
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
