using System.Collections.Generic;

namespace AdvantageTool.Models.UserRolePermissions
{
    public class Permission
    {
        public int PermissionId { get; set; }
        public string Name { get; set; }
        public int MvcControllerId { get; set; }
        public virtual MvcController MvcController { get; set; }
        public virtual ICollection<Role> Roles { get; set; }
    }
}
