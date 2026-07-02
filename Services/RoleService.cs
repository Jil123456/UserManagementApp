using UserManagementApp.Models;
using UserManagementApp.Repository;

namespace UserManagementApp.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repo;

        public RoleService(IRoleRepository repo)
        {
            _repo = repo;
        }

        public List<RoleMaster> GetAllRoles() => _repo.GetAllRoles();
        public RoleMaster? GetRoleById(int id) => _repo.GetRoleById(id);
        public void AddRole(string roleName) => _repo.AddRole(roleName);
        public void UpdateRole(int roleId, string roleName) => _repo.UpdateRole(roleId, roleName);
        public bool DeleteRole(int roleId) => _repo.DeleteRole(roleId);
        public int GetUserCountByRole(int roleId) => _repo.GetUserCountByRole(roleId);
    }
}
