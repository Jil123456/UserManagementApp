using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Filters;
using UserManagementApp.Services;
using UserManagementApp.Constants;
using UserManagementApp.Models;

namespace UserManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AdminDocController : Controller
    {
        private readonly IDocumentService _docService;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRoleService _roleService;

        public AdminDocController(IDocumentService docService, IUserService userService, IAuditLogService auditLogService, IRoleService roleService)
        {
            _docService = docService;
            _userService = userService;
            _auditLogService = auditLogService;
            _roleService = roleService;
        }

        // GET: View KYC Dashboard (Tabs: Pending & History)
        public IActionResult KycDashboard()
        {
            var adminId = HttpContext.Session.GetInt32("UserId");
            var adminRole = HttpContext.Session.GetInt32("RoleId");
            if (adminId == null || adminRole == null) return RedirectToAction("Login", "Auth", new { area = "" });

            if (adminRole != 3)
            {
                TempData["Error"] = "Access Denied: Only Super Admin can view the KYC Dashboard.";
                return RedirectToAction("Index", "Dashboard");
            }

            var allKycDocs = _docService.GetAllDocuments()
                .Where(d => d.DocumentType == "Aadhar Card" || d.DocumentType == "PAN Card")
                .ToList();

            var viewModel = new UserManagementApp.Models.KycDashboardViewModel();
            var roles = _roleService.GetAllRoles();

            foreach (var doc in allKycDocs)
            {
                var user = _userService.GetUserById(doc.UserId);
                if (user == null) continue;

                var roleName = roles.FirstOrDefault(r => r.RoleId == user.RoleId)?.RoleName ?? "User";

                var docRecord = new UserManagementApp.Models.KycDocumentRecord
                {
                    DocumentId = doc.DocumentId,
                    UserId = user.UserId,
                    Fullname = user.Fullname,
                    Username = user.Username,
                    Role = roleName,
                    DocumentType = doc.DocumentType,
                    FilePath = doc.FilePath,
                    UploadedDate = doc.UploadedDate,
                    Status = doc.Status,
                    ActionByAdmin = doc.ActionByAdmin,
                    ReviewedAt = doc.ReviewedAt,
                    RejectionReason = doc.RejectionReason
                };

                if (doc.Status == "Pending")
                {
                    viewModel.PendingKyc.Add(docRecord);
                }
                else
                {
                    viewModel.HistoryKyc.Add(docRecord);
                }
            }
            
            ViewBag.AllRoles = roles;

            return View(viewModel);
        }

        // POST: Approve documents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int docId)
        {
            var adminId = HttpContext.Session.GetInt32("UserId");
            var adminRole = HttpContext.Session.GetInt32("RoleId");
            if (adminId == null || adminRole == null) return RedirectToAction("Login", "Auth", new { area = "" });

            if (adminRole != 3)
            {
                TempData["Error"] = "Only Super Admin can approve KYC documents.";
                return RedirectToAction("KycDashboard");
            }

            var currentAdmin = HttpContext.Session.GetString("Username") ?? "Unknown";
            
            var doc = _docService.GetDocumentById(docId);
            if (doc != null)
            {
                _docService.UpdateDocumentStatus(docId, "Approved", currentAdmin, null);

                // Check if BOTH are approved
                var allUserDocs = _docService.GetDocumentsByUserId(doc.UserId);
                var aadhar = allUserDocs.FirstOrDefault(d => d.DocumentType == "Aadhar Card" && d.Status == "Approved");
                var pan = allUserDocs.FirstOrDefault(d => d.DocumentType == "PAN Card" && d.Status == "Approved");
                
                if (aadhar != null && pan != null)
                {
                    _userService.UpdateDocStatus(doc.UserId, "approved");
                    _userService.UpdateUserStatus(doc.UserId, "Approved", null, currentAdmin);
                    _auditLogService.LogAction(AuditActions.KYC_LOCKED, "System", "User", doc.UserId, "{\"reason\": \"Account approved and locked.\"}", "Info");
                }
                
                _auditLogService.LogAction(AuditActions.DOCUMENT_APPROVED, currentAdmin, "Document", docId, $"{{\"documentType\": \"{doc.DocumentType}\", \"oldStatus\": \"Pending\", \"newStatus\": \"Approved\", \"verifiedBy\": \"{currentAdmin}\"}}", "Info");
            }

            TempData["Success"] = "Documents Approved ✅";
            return RedirectToAction("KycDashboard");
        }

        // POST: Reject documents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int docId, string reason)
        {
            var adminId = HttpContext.Session.GetInt32("UserId");
            var adminRole = HttpContext.Session.GetInt32("RoleId");
            if (adminId == null || adminRole == null) return RedirectToAction("Login", "Auth", new { area = "" });

            if (adminRole != 3)
            {
                TempData["Error"] = "Only Super Admin can reject KYC documents.";
                return RedirectToAction("KycDashboard");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Rejection reason is mandatory.";
                return RedirectToAction("KycDashboard");
            }

            var currentAdmin = HttpContext.Session.GetString("Username") ?? "Unknown";

            var doc = _docService.GetDocumentById(docId);
            if (doc != null)
            {
                _docService.UpdateDocumentStatus(docId, "Rejected", currentAdmin, reason);
                _userService.UpdateDocStatus(doc.UserId, "rejected");
                _userService.UpdateUserStatus(doc.UserId, "Rejected", reason, currentAdmin);
                _userService.IncrementUploadAttempts(doc.UserId);

                // Check if locked
                var user = _userService.GetUserById(doc.UserId);
                if (user != null && user.UploadAttempts >= 3)
                {
                    _userService.DeactivateUser(doc.UserId);
                    _auditLogService.LogAction(AuditActions.KYC_LOCKED, "System", "User", doc.UserId, "{\"reason\": \"User locked after 3 failed KYC attempts.\"}", "Critical");
                }

                _auditLogService.LogAction(AuditActions.DOCUMENT_REJECTED, currentAdmin, "Document", docId, $"{{\"documentType\": \"{doc.DocumentType}\", \"oldStatus\": \"Pending\", \"newStatus\": \"Rejected\", \"reason\": \"{reason}\", \"verifiedBy\": \"{currentAdmin}\"}}", "Warning");
            }

            TempData["Error"] = "Documents Rejected ❌";
            return RedirectToAction("KycDashboard");
        }
    }
}
