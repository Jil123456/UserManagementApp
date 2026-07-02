using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string Fullname { get; set; } = null!;

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = null!;

        // No [Required] here — Password is only validated during registration
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mobile is required")]
        public string Mobile { get; set; } = null!;

        public DateTime Dob { get; set; }

        public int RoleId { get; set; }
        
        public string? RoleName { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; } = "Pending";

        public string? RejectReason { get; set; }

        public bool HasUnreadApproval { get; set; }

        public string? ActionByAdmin { get; set; }

        public bool IsActive { get; set; } = true;
        
        public bool IsDeleted { get; set; } = false;
        
        public string DocStatus { get; set; } = "pending";

        public int UploadAttempts { get; set; } = 0;
    }
}