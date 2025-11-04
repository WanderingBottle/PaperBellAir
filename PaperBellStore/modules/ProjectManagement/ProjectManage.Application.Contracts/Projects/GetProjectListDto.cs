using Volo.Abp.Application.Dtos;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 13:48:44
    /// Author: Tang
    /// </summary>
    public class GetProjectListDto : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 模糊查询：用于匹配Code、Name和Description
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// 精确查询：项目编码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 精确查询：项目名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 精确查询：项目描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 精确查询：项目状态
        /// </summary>
        public ProjectStatus? Status { get; set; }

        /// <summary>
        /// 精确查询：开始日期（起始）
        /// </summary>
        public DateTime? StartDateFrom { get; set; }

        /// <summary>
        /// 精确查询：开始日期（结束）
        /// </summary>
        public DateTime? StartDateTo { get; set; }

        /// <summary>
        /// 精确查询：结束日期（起始）
        /// </summary>
        public DateTime? EndDateFrom { get; set; }

        /// <summary>
        /// 精确查询：结束日期（结束）
        /// </summary>
        public DateTime? EndDateTo { get; set; }

        /// <summary>
        /// 精确查询：负责人名称
        /// </summary>
        public string OwnerName { get; set; }
    }
}
