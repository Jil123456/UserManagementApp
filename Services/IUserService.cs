using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        User? GetUserById(int id);
        User? GetUserByUsername(string username);
        void AddUser(User user);
        void UpdateUser(User user);
        void UpdatePassword(int userId, string hashedPassword);
        void UpdateUserStatus(int userId, string status, string? rejectReason = null, string? actionByAdmin = null);
        void UpdateUserUnreadApprovalStatus(int userId, bool status);
        bool DeleteUser(int userId);
        DashboardStats GetDashboardStats();
        int GetAdminCount();
        List<User> GetPendingUsers(int requestorRoleId);
        List<User> GetUsersByStatus(string status);
        void DeactivateUser(int userId);
        void ReactivateUser(int userId);
        void UpdateDocStatus(int userId, string docStatus);
        void IncrementUploadAttempts(int userId);
        void TransferUsersRole(int oldRoleId, int newRoleId);
    }
}
