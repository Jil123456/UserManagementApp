using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Filters;
using UserManagementApp.Models;
using UserManagementApp.Services;

namespace UserManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SessionAuth]
    public class DashboardController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAppealService _appealService;
        private readonly IDocumentService _documentService;
        private readonly IAuditLogService _auditLogService;

        public DashboardController(IUserService userService, IAppealService appealService, IDocumentService documentService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _appealService = appealService;
            _documentService = documentService;
            _auditLogService = auditLogService;
        }

        // GET: /Admin/Dashboard
        public IActionResult Index()
        {
            try
            {
                var stats = _userService.GetDashboardStats();
                var userId = HttpContext.Session.GetInt32("UserId");
                
                var rejectedDocsCount = 0;
                bool hasUnreadApproval = false;
                if (userId != null)
                {
                    var docs = _documentService.GetDocumentsByUserId(userId.Value);
                    rejectedDocsCount = docs.Count(d => d.Status == "Rejected");

                    var user = _userService.GetUserById(userId.Value);
                    if (user != null && user.HasUnreadApproval)
                    {
                        hasUnreadApproval = true;
                        _userService.UpdateUserUnreadApprovalStatus(userId.Value, false);
                    }
                }

                ViewBag.Username = HttpContext.Session.GetString("Username");
                ViewBag.TotalUsers = stats.TotalUsers;
                ViewBag.TotalAdmins = stats.TotalAdmins;
                ViewBag.TotalUsersOnly = stats.TotalStandardUsers;
                ViewBag.RejectedDocsCount = rejectedDocsCount;
                ViewBag.HasUnreadApproval = hasUnreadApproval;
            }
            catch (Exception ex)
            {
                ViewBag.Username = HttpContext.Session.GetString("Username");
                ViewBag.TotalUsers = 0;
                ViewBag.TotalAdmins = 0;
                ViewBag.TotalUsersOnly = 0;
                ViewBag.Error = "Database error: " + ex.Message;
            }

            return View();
        }

        // GET: /Admin/Dashboard/Profile
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth", new { area = "" });

            try
            {
                var user = _userService.GetUserById(userId.Value);
                if (user == null) return RedirectToAction("Index");

                return View(user);
            }
            catch (Exception)
            {
                return RedirectToAction("Index");
            }
        }

        // GET: /Admin/Dashboard/Appeals
        public IActionResult Appeals()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var user = _userService.GetUserById(userId.Value);
            if (user == null) return RedirectToAction("Index");

            var appeals = _appealService.GetAppealsByUserId(userId.Value);
            ViewBag.Appeals = appeals;
            return View(user);
        }

        // POST: /Admin/Dashboard/SendAppealMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendAppealMessage(string message)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth", new { area = "" });

            if (!string.IsNullOrWhiteSpace(message))
            {
                var appeal = new UserAppeal
                {
                    UserId = userId.Value,
                    Message = message,
                    SentBy = "User",
                    SentDate = DateTime.UtcNow
                };
                _appealService.AddAppeal(appeal);
            }
            return RedirectToAction("Appeals");
        }

    }
}
