using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AdvantageTool.Models.UserRolePermissions
{
    public class AuthenticatedUser
    {
        [DisplayName("Id")]
        [Required]
        public int AuthenticatedUserId { get; set; }

        [DisplayName("eConestoga UID")]
        [Required]
        public string UID { get; set; }

        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }

        [DisplayName("First Name")]
        public string GivenName { get; set; }

        [DisplayName("Last Name")]
        public string FamilyName { get; set; }
        
        public bool OverrideRole { get; set; }

        [DisplayName("Role")]
        public int RoleId { get; set; }

        public virtual Role Role { get; set; }

    }
}
