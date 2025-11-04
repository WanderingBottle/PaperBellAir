using Volo.Abp.Application.Dtos;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/11/4 13:46:52
    /// Author: Tang
    /// </summary>
    public class OwnerDto : FullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 负责人编码
        /// </summary>
        public string Code { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public OwnerDepartment Department { get; set; }
    }
}

