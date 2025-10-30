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
        public string Filter { get; set; }
        public ProjectStatus? Status { get; set; }
    }
}
