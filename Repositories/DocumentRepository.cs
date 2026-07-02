using Dapper;
using Npgsql;
using System.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly string _connectionString;

        public DocumentRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
            EnsureColumnsExist();
        }

        private void EnsureColumnsExist()
        {
            try
            {
                using var conn = Connection;
                conn.Execute(@"
                    ALTER TABLE userdocuments ADD COLUMN IF NOT EXISTS actionbyadmin VARCHAR(100);
                    ALTER TABLE userdocuments ADD COLUMN IF NOT EXISTS reviewed_at TIMESTAMP;
                    ALTER TABLE userdocuments ADD COLUMN IF NOT EXISTS rejectionreason TEXT;
                ");
                conn.Execute("UPDATE usermaster SET status = 'Approved' WHERE doc_status = 'approved' AND status != 'Approved';");
                conn.Execute("UPDATE usermaster SET status = 'Rejected' WHERE doc_status = 'rejected' AND status != 'Rejected';");

                // Recreate auditlogs table if it doesn't exist
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS auditlogs (
                        LogId SERIAL PRIMARY KEY,
                        ActionType VARCHAR(100) NOT NULL,
                        PerformedBy VARCHAR(100) NOT NULL,
                        EntityType VARCHAR(100) NOT NULL,
                        EntityId INT,
                        Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        Details JSONB,
                        IpAddress VARCHAR(45),
                        UserAgent TEXT,
                        Severity VARCHAR(50) DEFAULT 'Info'
                    );
                    ALTER TABLE auditlogs ADD COLUMN IF NOT EXISTS Severity VARCHAR(50) DEFAULT 'Info';
                    CREATE INDEX IF NOT EXISTS idx_auditlogs_entity ON auditlogs (EntityType, EntityId);
                    CREATE INDEX IF NOT EXISTS idx_auditlogs_timestamp ON auditlogs (Timestamp DESC);
                ");
            }
            catch { /* Ignore if it fails */ }
        }

        private IDbConnection Connection => new NpgsqlConnection(_connectionString);

        // ✅ Admin: Get all documents with user info
        public List<UserDocument> GetAllDocuments()
        {
            using var conn = Connection;
            return conn.Query<UserDocument>(@"
                SELECT d.*, u.username, u.fullname
                FROM userdocuments d
                JOIN usermaster u ON d.userid = u.userid
                ORDER BY d.uploadeddate DESC").ToList();
        }

        // ✅ User: Get my documents (latest first)
        public List<UserDocument> GetDocumentsByUserId(int userId)
        {
            using var conn = Connection;
            return conn.Query<UserDocument>(
                "SELECT * FROM userdocuments WHERE userid = @userId ORDER BY documentid DESC",
                new { userId }).ToList();
        }

        // ✅ Get single document
        public UserDocument? GetDocumentById(int documentId)
        {
            using var conn = Connection;
            return conn.QueryFirstOrDefault<UserDocument>(
                "SELECT * FROM userdocuments WHERE documentid = @documentId",
                new { documentId });
        }

        // ✅ Upload document
        public void AddDocument(UserDocument doc)
        {
            using var conn = Connection;
            conn.Execute(
                @"INSERT INTO userdocuments (userid, documenttype, filepath, aadhaar_path, pan_path, status, uploadeddate)
                  VALUES (@UserId, @DocumentType, @FilePath, @AadhaarPath, @PanPath, @Status, @UploadedDate)",
                doc);
        }

        // ✅ Approve / Reject
        public void UpdateDocumentStatus(int documentId, string status, string? actionByAdmin = null, string? rejectionReason = null)
        {
            using var conn = Connection;
            conn.Execute(
                "UPDATE userdocuments SET status = @status, actionbyadmin = @actionByAdmin, reviewed_at = NOW(), rejectionreason = @rejectionReason WHERE documentid = @documentId",
                new { documentId, status, actionByAdmin, rejectionReason });
        }

        // ✅ Delete document
        public void DeleteDocument(int documentId)
        {
            using var conn = Connection;
            conn.Execute(
                "DELETE FROM userdocuments WHERE documentid = @documentId",
                new { documentId });
        }

        // ✅ Get documents by status
        public List<UserDocument> GetDocumentsByStatus(string status)
        {
            using var conn = Connection;
            return conn.Query<UserDocument>(@"
                SELECT d.*, u.username, u.fullname
                FROM userdocuments d
                JOIN usermaster u ON d.userid = u.userid
                WHERE d.status = @status
                ORDER BY d.uploadeddate DESC",
                new { status }).ToList();
        }
    }
}
