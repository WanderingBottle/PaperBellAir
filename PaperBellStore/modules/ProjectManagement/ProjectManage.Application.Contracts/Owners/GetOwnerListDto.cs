using Volo.Abp.Application.Dtos;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/11/4 13:48:44
    /// Author: Tang
    /// </summary>
    public class GetOwnerListDto : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 模糊查询：用于匹配Name和Description
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// 精确查询：负责人名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 精确查询：负责人描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 精确查询：邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 精确查询：电话
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 精确查询：部门
        /// </summary>
        public OwnerDepartment? Department { get; set; }
    }
}

