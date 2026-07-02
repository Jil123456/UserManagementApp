using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Filters;
using UserManagementApp.Services;

namespace UserManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AuditController : Controller
    {
        private readonly IAuditLogService _auditLogService;

        public AuditController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public IActionResult Index(string? entityType, string? actionType, string? userFilter, string? detailsFilter, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                TempData["Error"] = "Access Denied: Super Admin Only.";
                return RedirectToAction("Index", "Dashboard");
            }

            var allLogsQuery = _auditLogService.GetAllLogs();

            if (!string.IsNullOrEmpty(entityType))
                allLogsQuery = allLogsQuery.Where(l => l.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(actionType))
                allLogsQuery = allLogsQuery.Where(l => l.ActionType.Equals(actionType, StringComparison.OrdinalIgnoreCase));
                
            if (!string.IsNullOrEmpty(userFilter))
                allLogsQuery = allLogsQuery.Where(l => l.PerformedBy.Contains(userFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(detailsFilter))
                allLogsQuery = allLogsQuery.Where(l => l.Details != null && l.Details.Contains(detailsFilter, StringComparison.OrdinalIgnoreCase));

            if (startDate.HasValue)
                allLogsQuery = allLogsQuery.Where(l => l.Timestamp.Date >= startDate.Value.Date);
                
            if (endDate.HasValue)
                allLogsQuery = allLogsQuery.Where(l => l.Timestamp.Date <= endDate.Value.Date);

            ViewBag.EntityTypes = _auditLogService.GetAllLogs().Select(l => l.EntityType).Distinct().OrderBy(e => e).ToList();
            ViewBag.ActionTypes = _auditLogService.GetAllLogs().Select(l => l.ActionType).Distinct().OrderBy(a => a).ToList();

            ViewBag.SelectedEntity = entityType;
            ViewBag.SelectedAction = actionType;
            ViewBag.SelectedUser = userFilter;
            ViewBag.SelectedDetails = detailsFilter;
            ViewBag.SelectedStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedEndDate = endDate?.ToString("yyyy-MM-dd");

            int pageSize = 20;
            int totalLogs = allLogsQuery.Count();
            int totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var pagedLogs = allLogsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalLogs = totalLogs;

            return View(pagedLogs);
        }
    }
}
