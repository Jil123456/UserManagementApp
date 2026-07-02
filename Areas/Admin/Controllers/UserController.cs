using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Filters;
using UserManagementApp.Models;
using UserManagementApp.Services;
using UserManagementApp.Constants;

namespace UserManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAppealService _appealService;
        private readonly IAuditLogService _auditLogService;

        public UserController(IUserService userService, IAppealService appealService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _appealService = appealService;
            _auditLogService = auditLogService;
        }

        // ✅ GET: /Admin/User — All Users List
        public IActionResult Index()
        {
            try
            {
                var users = _userService.GetAllUsers();
                return View(users);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Database error: " + ex.Message;
                return View(new List<User>());
            }
        }



        // ✅ GET: /Admin/User/Appeals/5 — View appeals for a rejected user
        public IActionResult Appeals(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
            {
                TempData["Error"] = "User not found ❌";
                return RedirectToAction("Index");
            }

            var appeals = _appealService.GetAppealsByUserId(id);
            ViewBag.Appeals = appeals;
            return View(user);
        }

        // ✅ POST: /Admin/User/SendAppealMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendAppealMessage(int userId, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                var appeal = new UserAppeal
                {
                    UserId = userId,
                    Message = message,
                    SentBy = "Admin",
                    SentDate = DateTime.UtcNow
                };
                _appealService.AddAppeal(appeal);
            }
            return RedirectToAction("Appeals", new { id = userId });
        }

        // ✅ GET: /Admin/User/Edit/5
        [AdminAuthorize]
        public IActionResult Edit(int id)
        {
            try
            {
                var user = _userService.GetUserById(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found ❌";
                    return RedirectToAction("Index");
                }
                
                // Fetch Timeline for this User
                ViewBag.AuditLogs = _auditLogService.GetLogsByEntity("User", id);
                
                return View(user);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading user: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ✅ POST: /Admin/User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public IActionResult Edit(int UserId, string Fullname, string Username, string Email, string Mobile)
        {
            try
            {
                var existing = _userService.GetUserByUsername(Username);
                if (existing != null && existing.UserId != UserId)
                {
                    TempData["Error"] = "Username already taken by another user ❌";
                    var currentUser = _userService.GetUserById(UserId);
                    return View(currentUser);
                }

                var user = _userService.GetUserById(UserId);
                if (user == null)
                {
                    TempData["Error"] = "User not found ❌";
                    return RedirectToAction("Index");
                }

                user.Fullname = Fullname;
                user.Username = Username;
                user.Email = Email;
                user.Mobile = Mobile;

                _userService.UpdateUser(user);

                var currentAdmin = HttpContext.Session.GetString("Username") ?? "Unknown";
                _auditLogService.LogAction(AuditActions.PROFILE_UPDATED, currentAdmin, "User", UserId, $"{{\"action\": \"Profile Updated\", \"fullname\": \"{user.Fullname}\", \"email\": \"{user.Email}\"}}", "Info");

                TempData["Success"] = "User updated successfully ✅";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating user: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ✅ POST: /Admin/User/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deactivate(int id)
        {
            try
            {
                var user = _userService.GetUserById(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found ❌";
                    return RedirectToAction("Index");
                }

                if (user.RoleId == 3)
                {
                    TempData["Error"] = "Super Admin cannot be deactivated! ⛔";
                    return RedirectToAction("Index");
                }

                if (user.RoleId == 1 && HttpContext.Session.GetInt32("RoleId") == 1)
                {
                    TempData["Error"] = "Access Denied: Admins cannot deactivate other Admins! ⛔";
                    return RedirectToAction("Index");
                }

                if (user.RoleId == 1)
                {
                    int adminCount = _userService.GetAdminCount();
                    if (adminCount <= 1)
                    {
                        TempData["Error"] = "Cannot deactivate the last admin! At least one admin must exist ⚠️";
                        return RedirectToAction("Index");
                    }
                }

                _userService.DeactivateUser(id);

                var currentAdmin = HttpContext.Session.GetString("Username") ?? "Unknown";
                _auditLogService.LogAction(AuditActions.USER_DEACTIVATED, currentAdmin, "User", id, $"{{\"action\": \"User Deactivated\", \"username\": \"{user.Username}\"}}", "Warning");

                TempData["Success"] = "User deactivated successfully ✅";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deactivating user: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ✅ POST: /Admin/User/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reactivate(int id)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 3)
                {
                    TempData["Error"] = "Access Denied: Only Super Admin can reactivate users.";
                    return RedirectToAction("Index");
                }

                var user = _userService.GetUserById(id);
                if (user != null)
                {
                    _userService.ReactivateUser(id);

                    var currentAdmin = HttpContext.Session.GetString("Username") ?? "Unknown";
                    _auditLogService.LogAction(AuditActions.USER_REACTIVATED, currentAdmin, "User", id, $"{{\"action\": \"User Reactivated\", \"username\": \"{user.Username}\"}}", "Info");

                    TempData["Success"] = "User reactivated successfully ✅";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error reactivating user: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Delete method has been completely removed to prevent both Admins and Super Admins from deleting users
        // ✅ GET: /Admin/User/RejectedHistory
        public IActionResult RejectedHistory()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                TempData["Error"] = "Access Denied: Super Admin Only.";
                return RedirectToAction("Index", "Dashboard");
            }
            var users = _userService.GetUsersByStatus("Rejected");
            return View(users);
        }

        // ✅ GET: /Admin/User/ApprovedHistory
        public IActionResult ApprovedHistory()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                TempData["Error"] = "Access Denied: Super Admin Only.";
                return RedirectToAction("Index", "Dashboard");
            }
            var users = _userService.GetUsersByStatus("Approved");
            return View(users);
        }
    }
}
