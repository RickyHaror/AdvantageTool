using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AdvantageTool.Models.UserRolePermissions;

namespace AdvantageTool.Models
{
    class ControllerPermissions
    {
        public string ControllerName { get; set; }
        public List<string> Permissions { get; set; }
    }
    public class DbInitializer
    {
        public void Initialize(ContextDb context)
        {
            if (context.Database.GetService<IRelationalDatabaseCreator>().Exists())
            {
                // Add all controller permissions here
                var newControllerPermissions = new List<ControllerPermissions>()
                {
                    new ControllerPermissions{ControllerName = "home", Permissions = new List<string>{"create", "view", "edit", "delete"}},
                    new ControllerPermissions{ControllerName = "tool", Permissions = new List<string>{"view"}},
                };

                InitializeControllerPermissions(newControllerPermissions, context);
            }
        }

        // Add Controller-Permissions to database
        private void InitializeControllerPermissions(List<ControllerPermissions> controllerPermissions, ContextDb context)
        {
            foreach (var controllerPermission in controllerPermissions)
            {

                // If controller already exsits, update it with permissions
                if (context.MvcControllers.Any(c => c.Name == controllerPermission.ControllerName))
                {
                    var mvcController = context.MvcControllers.Include(p => p.Permissions).Single(c => c.Name == controllerPermission.ControllerName);
                    foreach (var permission in controllerPermission.Permissions)
                    {
                        // Add if permission does not already exist for controller
                        if (mvcController.Permissions?.Any(p => p.Name == permission) != true)
                        {
                            var newPermission = new Permission { Name = permission, MvcController = mvcController };
                            context.Permissions.Add(newPermission);
                            mvcController.Permissions.Add(newPermission);
                        }
                    }
                    context.MvcControllers.Update(mvcController);
                }
                // Else create a new controller and permissions
                else
                {
                    var mvcController = new MvcController
                    {
                        Name = controllerPermission.ControllerName,
                        Permissions = new Collection<Permission>(),
                    };
                    foreach (var permission in controllerPermission.Permissions)
                    {
                        if (!mvcController.Permissions.Any(p => p.Name == permission))
                        {
                            var newPermission = new Permission { Name = permission, MvcController = mvcController };
                            context.Permissions.Add(newPermission);
                            mvcController.Permissions.Add(newPermission);
                        }
                    }
                    context.MvcControllers.Add(mvcController);
                }
            }
            context.SaveChanges();
        }       
    }
}