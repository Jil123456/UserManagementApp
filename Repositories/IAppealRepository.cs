using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public interface IAppealRepository
    {
        void AddAppeal(UserAppeal appeal);
        List<UserAppeal> GetAppealsByUserId(int userId);
    }
}
