using Volo.Abp.Application.Dtos;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 13:46:52
    /// Author: Tang
    /// </summary>
    public class ProjectDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus Status { get; set; }
        public Guid? OwnerId { get; set; }
        public string OwnerName { get; set; }
    }
}
