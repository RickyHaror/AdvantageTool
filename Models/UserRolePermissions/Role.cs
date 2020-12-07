using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdvantageTool.Models.UserRolePermissions
{
    public class Role
    {
        public int RoleId { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Name { get; set; }

        public virtual IList<Permission> Permissions { get; set; }
    }
}
