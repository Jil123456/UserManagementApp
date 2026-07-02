using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Filters;
using UserManagementApp.Services;
using UserManagementApp.Constants;

namespace UserManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        public RoleController(IRoleService roleService, IUserService userService, IAuditLogService auditLogService)
        {
            _roleService = roleService;
            _userService = userService;
            _auditLogService = auditLogService;
        }

        // GET: /Admin/Role
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                TempData["Error"] = "Access Denied: Super Admin Only.";
                return RedirectToAction("Index", "Dashboard");
            }
            try
            {
                var roles = _roleService.GetAllRoles();
                return View(roles);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading roles: " + ex.Message;
                return View(new List<UserManagementApp.Models.RoleMaster>());
            }
        }

        // POST: /Admin/Role/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string RoleName)
        {
            if (string.IsNullOrWhiteSpace(RoleName))
            {
                TempData["Error"] = "Role name cannot be empty ❌";
                return RedirectToAction("Index");
            }

            try
            {
                _roleService.AddRole(RoleName.Trim());
                var currentAdmin = HttpContext.Session.GetString("Username") ?? "System";
                _auditLogService.LogAction(AuditActions.ROLE_CREATED, currentAdmin, "Role", null, $"{{\"RoleName\": \"{RoleName}\"}}");
                TempData["Success"] = "Role '" + RoleName + "' added successfully ✅";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    TempData["Error"] = "Role '" + RoleName + "' already exists ❌";
                else
                    TempData["Error"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Admin/Role/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int RoleId, string RoleName)
        {
            if (string.IsNullOrWhiteSpace(RoleName))
            {
                TempData["Error"] = "Role name cannot be empty ❌";
                return RedirectToAction("Index");
            }

            if (RoleId <= 3)
            {
                TempData["Error"] = "Core system roles cannot be modified! ⛔";
                return RedirectToAction("Index");
            }

            try
            {
                _roleService.UpdateRole(RoleId, RoleName.Trim());
                var currentAdmin = HttpContext.Session.GetString("Username") ?? "System";
                _auditLogService.LogAction(AuditActions.ROLE_UPDATED, currentAdmin, "Role", RoleId, $"{{\"RoleName\": \"{RoleName}\"}}");
                TempData["Success"] = "Role updated successfully ✅";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
                    TempData["Error"] = "Role name '" + RoleName + "' already exists ❌";
                else
                    TempData["Error"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Admin/Role/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int? transferRoleId)
        {
            try
            {
                if (id <= 3)
                {
                    TempData["Error"] = "Core system roles cannot be deleted! ⛔";
                    return RedirectToAction("Index");
                }

                int userCount = _roleService.GetUserCountByRole(id);
                if (userCount > 0)
                {
                    if (!transferRoleId.HasValue || transferRoleId.Value == id)
                    {
                        TempData["Error"] = $"Cannot delete this role! {userCount} user(s) are assigned to it. Please select a valid role to transfer them to. ⚠️";
                        return RedirectToAction("Index");
                    }
                    else if (transferRoleId.Value == 3)
                    {
                        TempData["Error"] = "Cannot transfer users to the Super Admin role! ⛔";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Transfer users
                        _userService.TransferUsersRole(id, transferRoleId.Value);
                    }
                }

                _roleService.DeleteRole(id);
                var currentAdmin = HttpContext.Session.GetString("Username") ?? "System";
                var transferredStr = transferRoleId.HasValue ? "Yes" : "No";
                string transferDetails = transferRoleId.HasValue ? $", \"transferredToRoleId\": \"{transferRoleId.Value}\"" : "";
                _auditLogService.LogAction(AuditActions.ROLE_DELETED, currentAdmin, "Role", id, $"{{\"deletedRoleId\": \"{id}\", \"usersTransferred\": \"{transferredStr}\"{transferDetails}}}", "Warning");
                TempData["Success"] = "Role deleted successfully ✅";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
