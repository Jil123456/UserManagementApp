namespace UserManagementApp.Models
{
    public class AuditLog
    {
        public int LogId { get; set; }
        public string ActionType { get; set; } = null!;
        public string PerformedBy { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public int? EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string Severity { get; set; } = "Info";
    }
}
