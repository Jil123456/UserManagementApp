using Dapper;
using Npgsql;
using System.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public class RoleRepository : IRoleRepository
    {
        private readonly string _connectionString;

        public RoleRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        private IDbConnection Connection => new NpgsqlConnection(_connectionString);

        public List<RoleMaster> GetAllRoles()
        {
            using var conn = Connection;
            return conn.Query<RoleMaster>("SELECT * FROM rolemaster ORDER BY roleid").ToList();
        }

        public RoleMaster? GetRoleById(int id)
        {
            using var conn = Connection;
            return conn.QueryFirstOrDefault<RoleMaster>(
                "SELECT * FROM rolemaster WHERE roleid = @id", new { id });
        }

        public void AddRole(string roleName)
        {
            using var conn = Connection;
            conn.Execute(
                "INSERT INTO rolemaster (rolename) VALUES (@roleName)",
                new { roleName });
        }

        public void UpdateRole(int roleId, string roleName)
        {
            using var conn = Connection;
            conn.Execute(
                "UPDATE rolemaster SET rolename = @roleName WHERE roleid = @roleId",
                new { roleId, roleName });
        }

        public bool DeleteRole(int roleId)
        {
            using var conn = Connection;
            var rows = conn.Execute(
                "DELETE FROM rolemaster WHERE roleid = @roleId",
                new { roleId });
            return rows > 0;
        }

        public int GetUserCountByRole(int roleId)
        {
            using var conn = Connection;
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM usermaster WHERE roleid = @roleId",
                new { roleId });
        }
    }
}
