using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementApp.Models
{
    [Table("usermaster")] // ✅ CORRECT PLACE
    public class User
    {
        [Key]
        [Column("userid")]
        public int UserId { get; set; }

        [Required]
        [Column("fullname")]
        public string Fullname { get; set; } = null!;

        [Required]
        [Column("username")]
        public string Username { get; set; } = null!;
        [Required]
        [StringLength(200)]
        [Column("password")]
        public string Password { get; set; } = null!;

        [Required]
        [Column("email")]
        public string Email { get; set; } = null!;

        [Required]
        [Column("mobile")]
        public string Mobile { get; set; } = null!;

        [Column("dob")]
        public DateTime Dob { get; set; }

        [Column("roleid")]
        public int RoleId { get; set; }  // 🔥 IMPORTANT CHANGE

        [Column("createddate")]
        public DateTime CreatedDate { get; set; }
    }
}