using Dapper;
using Npgsql;
using System.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public class AppealRepository : IAppealRepository
    {
        private readonly string _connectionString;

        public AppealRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        private IDbConnection Connection => new NpgsqlConnection(_connectionString);

        public void AddAppeal(UserAppeal appeal)
        {
            using var conn = Connection;
            conn.Execute(
                @"INSERT INTO userappeals (userid, message, sentby, sentdate)
                  VALUES (@UserId, @Message, @SentBy, @SentDate)",
                appeal);
        }

        public List<UserAppeal> GetAppealsByUserId(int userId)
        {
            using var conn = Connection;
            return conn.Query<UserAppeal>(
                "SELECT * FROM userappeals WHERE userid = @userId ORDER BY sentdate ASC",
                new { userId }).ToList();
        }
    }
}
