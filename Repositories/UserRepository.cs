using Dapper;
using Npgsql;
using System.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        private IDbConnection Connection => new NpgsqlConnection(_connectionString);

        // ✅ GET ALL USERS
        public List<User> GetAllUsers()
        {
            using var conn = Connection;
            return conn.Query<User>(@"
                SELECT u.*, r.rolename as RoleName 
                FROM usermaster u 
                LEFT JOIN rolemaster r ON u.roleid = r.roleid 
                ORDER BY u.userid").ToList();
        }

        // ✅ GET USER BY ID
        public User? GetUserById(int id)
        {
            using var conn = Connection;
            return conn.QueryFirstOrDefault<User>(
                "SELECT * FROM usermaster WHERE userid = @id",
                new { id });
        }

        // ✅ GET USER BY USERNAME
        public User? GetUserByUsername(string username)
        {
            using var conn = Connection;
            return conn.QueryFirstOrDefault<User>(
                "SELECT * FROM usermaster WHERE username = @username",
                new { username });
        }

        // ✅ ADD USER (with status = 'Pending')
        public void AddUser(User user)
        {
            using var conn = Connection;
            conn.Execute(
                @"INSERT INTO usermaster (fullname, username, password, email, mobile, dob, roleid, createddate, status)
                  VALUES (@Fullname, @Username, @Password, @Email, @Mobile, @Dob, @RoleId, @CreatedDate, @Status)",
                user);
        }

        // ✅ UPDATE USER
        public void UpdateUser(User user)
        {
            using var conn = Connection;
            conn.Execute(
                @"UPDATE usermaster 
                  SET fullname = @Fullname, username = @Username, email = @Email, mobile = @Mobile
                  WHERE userid = @UserId",
                user);
        }

        // ✅ UPDATE PASSWORD
        public void UpdatePassword(int userId, string hashedPassword)
        {
            using var conn = Connection;
            conn.Execute(
                "UPDATE usermaster SET password = @hashedPassword WHERE userid = @userId",
                new { userId, hashedPassword });
        }

        // ✅ UPDATE USER STATUS (Approve / Reject)
        public void UpdateUserStatus(int userId, string status, string? rejectReason = null, string? actionByAdmin = null)
        {
            using var conn = Connection;
            conn.Execute(
                "UPDATE usermaster SET status = @status, rejectreason = @rejectReason, actionbyadmin = @actionByAdmin WHERE userid = @userId",
                new { userId, status, rejectReason, actionByAdmin });
        }

        // ✅ DELETE USER
        public bool DeleteUser(int userId)
        {
            using var conn = Connection;
            var rows = conn.Execute(
                "DELETE FROM usermaster WHERE userid = @userId",
                new { userId });
            return rows > 0;
        }

        // ✅ DASHBOARD STATS
        public DashboardStats GetDashboardStats()
        {
            try
            {
                using var conn = Connection;
                var stats = conn.QueryFirstOrDefault<DashboardStats>(@"
                    SELECT 
                        COUNT(*) FILTER (WHERE status = 'Approved')::INT AS totalusers,
                        COUNT(*) FILTER (WHERE roleid = 1 AND status = 'Approved')::INT AS totaladmins,
                        COUNT(*) FILTER (WHERE roleid = 2 AND status = 'Approved')::INT AS totalstandardusers
                    FROM usermaster");
                return stats ?? new DashboardStats();
            }
            catch
            {
                return new DashboardStats();
            }
        }

        // ✅ ADMIN COUNT (only approved admins)
        public int GetAdminCount()
        {
            using var conn = Connection;
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM usermaster WHERE roleid = 1 AND status = 'Approved'");
        }

        // o. GET PENDING USERS (waiting for approval)
        public List<User> GetPendingUsers(int requestorRoleId)
        {
            using var conn = Connection;
            if (requestorRoleId == 3)
            {
                // Super Admin sees ALL pending users (both Roles 1 and 2)
                return conn.Query<User>("SELECT * FROM usermaster WHERE status = 'Pending' ORDER BY createddate DESC").ToList();
            }
            else
            {
                // Normal Admin sees ONLY pending standard users (Role 2)
                return conn.Query<User>("SELECT * FROM usermaster WHERE status = 'Pending' AND roleid = 2 ORDER BY createddate DESC").ToList();
            }
        }

        // o. GET USERS BY STATUS
        public List<User> GetUsersByStatus(string status)
        {
            using var conn = Connection;
            return conn.Query<User>("SELECT * FROM usermaster WHERE status = @status ORDER BY createddate DESC", new { status }).ToList();
        }

        // o. MARK UNREAD APPROVAL
        public void UpdateUserUnreadApprovalStatus(int userId, bool status)
        {
            using var conn = Connection;
            conn.Execute(
                "UPDATE usermaster SET has_unread_approval = @Status WHERE userid = @UserId",
                new { Status = status, UserId = userId });
        }

        public void DeactivateUser(int userId)
        {
            using var conn = Connection;
            conn.Execute("UPDATE usermaster SET is_deleted = true, is_active = false WHERE userid = @userId", new { userId });
        }

        public void ReactivateUser(int userId)
        {
            using var conn = Connection;
            conn.Execute("UPDATE usermaster SET is_deleted = false, is_active = true, upload_attempts = 0, doc_status = 'pending' WHERE userid = @userId", new { userId });
        }

        public void UpdateDocStatus(int userId, string docStatus)
        {
            using var conn = Connection;
            conn.Execute("UPDATE usermaster SET doc_status = @docStatus WHERE userid = @userId", new { userId, docStatus });
        }

        public void IncrementUploadAttempts(int userId)
        {
            using var conn = Connection;
            conn.Execute("UPDATE usermaster SET upload_attempts = upload_attempts + 1 WHERE userid = @userId", new { userId });
        }

        // ✅ TRANSFER USERS ROLE
        public void TransferUsersRole(int oldRoleId, int newRoleId)
        {
            using var conn = Connection;
            conn.Execute(
                "UPDATE usermaster SET roleid = @newRoleId WHERE roleid = @oldRoleId",
                new { newRoleId, oldRoleId });
        }
    }
}