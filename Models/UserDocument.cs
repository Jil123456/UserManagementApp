namespace UserManagementApp.Models
{
    public class UserDocument
    {
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public string DocumentType { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string Status { get; set; } = "Pending";
        public DateTime UploadedDate { get; set; }

        // For display only (not in DB)
        public string? Username { get; set; }
        public string? Fullname { get; set; }

        // Extract filename from filepath for display
        public string DisplayFileName => Path.GetFileName(FilePath ?? "");

        public string? ActionByAdmin { get; set; }

        public string? AadhaarPath { get; set; }
        public string? PanPath { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public string? RejectionReason { get; set; }
    }
}
