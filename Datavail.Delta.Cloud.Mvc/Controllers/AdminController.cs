using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Cloud.Mvc.Models.Admin;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Cloud.Mvc.Models.Config;
using Datavail.Delta.Cloud.Mvc.Infrastructure;
using Datavail.Delta.Cloud.Mvc.Utility;
using Datavail.Delta.Cloud.Mvc.Models;
using AutoMapper;
using System.Text;

namespace Datavail.Delta.Cloud.Mvc.Controllers
{
    [Authorize(Roles=Constants.DELTAADMIN)]
    public class AdminController : DeltaController
    {
        #region "private variables"
        private IServerService _serverService;
        #endregion

        #region "CTOR"
        public AdminController(IServerService serverService)
        {
            _serverService = serverService;
        }
        #endregion

        public ActionResult UserMaintenance()
        {
            var usersPageModel = InitializeUserMaintenanceModel();

            return View(usersPageModel);
        }

        #region "Users"
        [HttpPost]
        public JsonResult UserDelete(Guid userId)
        {
            var userIds = new List<Guid>();
            userIds.Add(userId);

            if (!_serverService.DeleteUsers(userIds))
            {
                return Json(new { success = false, errors = new string[] { "The User Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult UserEdit(Guid? userId)
        {
            var userModel = new UserModel();
            var potentialRoles = _serverService.GetRoles();

            if (userId != null)
            {
                //Edit
                var user = _serverService.GetByKey<User>(userId.Value);

                Mapper.CreateMap<User, UserModel>();
                userModel = Mapper.Map<User, UserModel>(user);

                userModel.RoleModel.PotentialRoles = potentialRoles;
                userModel.RoleModel.SelectedRoleIds = user.Roles.Select(x => x.Id);
                userModel.RoleModel.PotentialRolesHeader = "Application Roles";
                userModel.RoleModel.SelectedRolesHeader = "Selected Roles";

                return View(userModel);
            }

            userModel.RoleModel.PotentialRoles = potentialRoles;
            userModel.RoleModel.PotentialRolesHeader = "Application Roles";
            userModel.RoleModel.SelectedRolesHeader = "Selected Roles";

            return View(userModel);
        }

        [HttpPost]
        public JsonResult UserEdit(UserModel userModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<UserModel, User>();
            var user = Mapper.Map<UserModel, User>(userModel);

            if (!string.IsNullOrEmpty(userModel.Password))
            {
                user.SetPassword(userModel.Password);
            }

            //Validate
            if (!_serverService.ValidateUser(user, out serverErrors))
            {
                foreach (var error in serverErrors)
                {
                    ModelState.AddModelError("Validation", error);
                }
            }
            else
            {
                if (!_serverService.SaveUser(ref user, userModel.RoleModel.SelectedRoleIds.ToList()))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }
      
        [HttpGet]
        public JsonResult UsersTable(string sidx, string sord, int page, int rows,
                                            bool _search, string searchField, string searchOper, string searchString)
        {
            IEnumerable<User> users = new List<User>();
            Specification<User> criteria = new Specification<User>(x => !string.IsNullOrEmpty(x.EmailAddress));

            if (_search)
            {
                criteria = criteria.And(x => x.FirstName.Contains(searchString) || x.LastName.Contains(searchString) || x.EmailAddress.Contains(searchString));
            }

            var totalRecords = _serverService.GetPagedEntities<User>(page, rows, criteria, x => x.EmailAddress, out users);
            var totalPages = (int)Math.Ceiling((float)totalRecords / (float)rows);

            var result = new
            {
                total = totalPages,
                page = page,
                records = totalRecords,
                rows = users.Select(x => new { x.Id, x.EmailAddress, x.FirstName, x.LastName, x.PasswordHash, x.PasswordSalt })
                            .ToList()
                            .Select(x => new
                            {
                                id = x.Id.ToString(),
                                cell = new string[] {
                            GetActionItems(new List<ActionModel> {
                                new ActionModel { 
                                    Id =x.Id.ToString(), 
                                    Title="Edit User",
                                    Url = "Admin/UserEdit?userid=" + x.Id.ToString(),
                                    Class = "edit-row-button", 
                                    Icon=Constants.EDITICON, 
                                    Alt="Edit"
                                },
                                new ActionModel { 
                                    Id =x.Id.ToString(), 
                                    Title="Delete User",
                                    Url = "Admin/UserDelete?userid=" + x.Id.ToString(),
                                    Class = "delete-row-button", 
                                    Icon=Constants.DELETEICON, 
                                    Alt="Delete"
                                }
                            }),
                            x.EmailAddress,
                            x.FirstName,
                            x.LastName,
                            string.Empty
                        }
                            })
                            .ToArray(),
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region "Metrics"
        
        #endregion

        #region "private methods"
        private UserMaintenanceModel InitializeUserMaintenanceModel()
        {
            var usersMaintenanceModel = new UserMaintenanceModel();
            usersMaintenanceModel.UsersTable = GetTableModel(Constants.TableType.User, TenantId, false, false, true);

            //Toolbar
            usersMaintenanceModel.UsersTable.ToolbarItems.Add(new ToolbarItemModel
            {
                ToolbarItemId = "add-user-button",
                ToolbarItemIconUrl = "~/Content/images/navicons-small/171.png",
                ToolbarItemUrl = "Admin/UserEdit/",
                ToolbarItemTitle = "Add User",
                ToolbarItemAction = "",
                ToolbarItemClass = "small-button add-button"
            });

            return usersMaintenanceModel;
        }
        #endregion
    }
}
