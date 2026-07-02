using UserManagementApp.Models;
using System.Collections.Generic;

namespace UserManagementApp.Services
{
    public interface IAuditLogService
    {
        void LogAction(string actionType, string performedBy, string entityType, int? entityId, string? detailsJson = null, string severity = "Info");
        IEnumerable<AuditLog> GetAllLogs();
        IEnumerable<AuditLog> GetLogsByEntity(string entityType, int entityId);
    }
}
