using System.ComponentModel.DataAnnotations;

namespace ProjectManage.Owners
{
    /// <summary>
    /// Description:
    /// CreateTime: 2025/11/4 13:47:54
    /// Author: Tang
    /// </summary>
    public class CreateUpdateOwnerDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(2048)]
        public string Description { get; set; }

        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(32)]
        public string Phone { get; set; }

        public OwnerDepartment Department { get; set; } = OwnerDepartment.ResearchAndDevelopment;
    }
}

