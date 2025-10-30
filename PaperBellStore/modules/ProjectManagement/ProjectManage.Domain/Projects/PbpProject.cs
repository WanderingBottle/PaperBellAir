using Volo.Abp.Domain.Entities.Auditing;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 13:25:50
    /// Author: Tang
    /// </summary>
    public class PbpProject : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus Status { get; set; }
        public Guid? OwnerId { get; set; }
        public Guid? TenantId { get; set; }

        protected PbpProject() { }

        public PbpProject(Guid id , string name , string descrption = null)
            : base(id)
        {
            this.Name=name;
            this.Description=descrption;
            this.Status=ProjectStatus.Planning;
        }
    }
}
