using System.ComponentModel.DataAnnotations;

namespace ProjectManage.Projects
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/10/30 13:47:54
    /// Author: Tang
    /// </summary>
    public class CreateUpdateProjectDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(2048)]
        public string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        public Guid? OwnerId { get; set; }
    }
}
