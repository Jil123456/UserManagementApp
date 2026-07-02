using UserManagementApp.Models;

namespace UserManagementApp.Services
{
    public interface IAppealService
    {
        void AddAppeal(UserAppeal appeal);
        List<UserAppeal> GetAppealsByUserId(int userId);
    }
}
