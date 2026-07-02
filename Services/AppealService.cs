using UserManagementApp.Models;
using UserManagementApp.Repository;

namespace UserManagementApp.Services
{
    public class AppealService : IAppealService
    {
        private readonly IAppealRepository _repo;

        public AppealService(IAppealRepository repo)
        {
            _repo = repo;
        }

        public void AddAppeal(UserAppeal appeal)
        {
            _repo.AddAppeal(appeal);
        }

        public List<UserAppeal> GetAppealsByUserId(int userId)
        {
            return _repo.GetAppealsByUserId(userId);
        }
    }
}
