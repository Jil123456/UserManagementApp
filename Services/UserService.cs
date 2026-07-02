using UserManagementApp.Models;
using UserManagementApp.Repository;

namespace UserManagementApp.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public List<User> GetAllUsers()
        {
            return _repo.GetAllUsers();
        }

        public User? GetUserById(int id)
        {
            return _repo.GetUserById(id);
        }

        public User? GetUserByUsername(string username)
        {
            return _repo.GetUserByUsername(username);
        }

        public void AddUser(User user)
        {
            _repo.AddUser(user);
        }

        public void UpdateUser(User user)
        {
            _repo.UpdateUser(user);
        }

        public void UpdatePassword(int userId, string hashedPassword)
        {
            _repo.UpdatePassword(userId, hashedPassword);
        }

        public void UpdateUserStatus(int userId, string status, string? rejectReason = null, string? actionByAdmin = null)
        {
            _repo.UpdateUserStatus(userId, status, rejectReason, actionByAdmin);
        }

        public bool DeleteUser(int userId)
        {
            return _repo.DeleteUser(userId);
        }

        public DashboardStats GetDashboardStats()
        {
            return _repo.GetDashboardStats();
        }

        public int GetAdminCount()
        {
            return _repo.GetAdminCount();
        }

        public List<User> GetPendingUsers(int requestorRoleId)
        {
            return _repo.GetPendingUsers(requestorRoleId);
        }

        public void UpdateUserUnreadApprovalStatus(int userId, bool status)
        {
            _repo.UpdateUserUnreadApprovalStatus(userId, status);
        }

        public List<User> GetUsersByStatus(string status)
        {
            return _repo.GetUsersByStatus(status);
        }

        public void DeactivateUser(int userId)
        {
            _repo.DeactivateUser(userId);
        }

        public void ReactivateUser(int userId)
        {
            _repo.ReactivateUser(userId);
        }

        public void UpdateDocStatus(int userId, string docStatus)
        {
            _repo.UpdateDocStatus(userId, docStatus);
        }

        public void IncrementUploadAttempts(int userId)
        {
            _repo.IncrementUploadAttempts(userId);
        }

        public void TransferUsersRole(int oldRoleId, int newRoleId)
        {
            _repo.TransferUsersRole(oldRoleId, newRoleId);
        }
    }
}
