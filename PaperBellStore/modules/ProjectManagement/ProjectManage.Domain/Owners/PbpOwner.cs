using Volo.Abp.Domain.Entities.Auditing;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/11/4 13:25:51
    /// Author: Tang
    /// </summary>
    public class PbpOwner : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public OwnerDepartment Department { get; set; }
        public Guid? TenantId { get; set; }

        protected PbpOwner() { }

        public PbpOwner(Guid id, string name, string description = null)
            : base(id)
        {
            this.Name = name;
            this.Description = description;
        }
    }
}
