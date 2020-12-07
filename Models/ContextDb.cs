using AdvantageTool.Models.UserRolePermissions;
using Microsoft.EntityFrameworkCore;

namespace AdvantageTool.Models
{
    public class ContextDb : DbContext
    {
        public ContextDb(DbContextOptions<ContextDb> options) : base(options)
        {
        }

        public DbSet<AuthenticatedUser> AuthenticatedUsers { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<MvcController> MvcControllers { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
        }
    }
}
