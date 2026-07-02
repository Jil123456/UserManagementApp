namespace UserManagementApp.Models
{
    public class UserAppeal
    {
        public int AppealId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = null!;
        public string SentBy { get; set; } = "User"; // "User" or "Admin"
        public DateTime SentDate { get; set; }
    }
}
