using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public interface IRoleRepository
    {
        List<RoleMaster> GetAllRoles();
        RoleMaster? GetRoleById(int id);
        void AddRole(string roleName);
        void UpdateRole(int roleId, string roleName);
        bool DeleteRole(int roleId);
        int GetUserCountByRole(int roleId);
    }
}
