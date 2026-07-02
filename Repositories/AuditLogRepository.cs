using Npgsql;
using UserManagementApp.Models;
using UserManagementApp.Services;

namespace UserManagementApp.Repositories
{
    public class AuditLogRepository : IAuditLogService
    {
        private readonly string _connectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogRepository(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _httpContextAccessor = httpContextAccessor;
        }

        public void LogAction(string actionType, string performedBy, string entityType, int? entityId, string? detailsJson = null, string severity = "Info")
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            string? ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            string? userAgent = request?.Headers["User-Agent"].ToString();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO auditlogs (ActionType, PerformedBy, EntityType, EntityId, Details, IpAddress, UserAgent, Severity) " +
                    "VALUES (@action, @performedBy, @entityType, @entityId, @details::jsonb, @ipAddress, @userAgent, @severity)", conn))
                {
                    cmd.Parameters.AddWithValue("action", actionType);
                    cmd.Parameters.AddWithValue("performedBy", performedBy);
                    cmd.Parameters.AddWithValue("entityType", entityType);
                    cmd.Parameters.AddWithValue("entityId", (object?)entityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("details", (object?)detailsJson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("ipAddress", (object?)ipAddress ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("userAgent", (object?)userAgent ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("severity", severity);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<AuditLog> GetAllLogs()
        {
            var logs = new List<AuditLog>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT * FROM auditlogs ORDER BY Timestamp DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(MapToAuditLog(reader));
                    }
                }
            }
            return logs;
        }

        public IEnumerable<AuditLog> GetLogsByEntity(string entityType, int entityId)
        {
            var logs = new List<AuditLog>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT * FROM auditlogs WHERE EntityType = @entityType AND EntityId = @entityId ORDER BY Timestamp DESC", conn))
                {
                    cmd.Parameters.AddWithValue("entityType", entityType);
                    cmd.Parameters.AddWithValue("entityId", entityId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(MapToAuditLog(reader));
                        }
                    }
                }
            }
            return logs;
        }

        private AuditLog MapToAuditLog(NpgsqlDataReader reader)
        {
            return new AuditLog
            {
                LogId = Convert.ToInt32(reader["LogId"]),
                ActionType = reader["ActionType"].ToString() ?? "",
                PerformedBy = reader["PerformedBy"].ToString() ?? "",
                EntityType = reader["EntityType"].ToString() ?? "",
                EntityId = reader["EntityId"] != DBNull.Value ? Convert.ToInt32(reader["EntityId"]) : null,
                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                Details = reader["Details"] != DBNull.Value ? reader["Details"].ToString() : null,
                IpAddress = reader["IpAddress"] != DBNull.Value ? reader["IpAddress"].ToString() : null,
                UserAgent = reader["UserAgent"] != DBNull.Value ? reader["UserAgent"].ToString() : null,
                Severity = reader["Severity"] != DBNull.Value ? reader["Severity"].ToString()! : "Info"
            };
        }
    }
}
