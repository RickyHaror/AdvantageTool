using System.Collections.Generic;

namespace AdvantageTool.Models.UserRolePermissions
{
    public class MvcController
    {
        public int MvcControllerId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Permission> Permissions { get; set; }
    }
}
