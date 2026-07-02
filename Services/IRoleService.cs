using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public interface IRoleService
    {
        List<RoleMaster> GetAllRoles();
        RoleMaster? GetRoleById(int id);
        void AddRole(string roleName);
        void UpdateRole(int roleId, string roleName);
        bool DeleteRole(int roleId);
        int GetUserCountByRole(int roleId);
    }
}
