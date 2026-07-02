namespace UserManagementApp.Models
{
    public class KycDocumentRecord
    {
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public string Fullname { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Role { get; set; } = "User";
        
        public string DocumentType { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        
        public DateTime UploadedDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ActionByAdmin { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class KycDashboardViewModel
    {
        public List<KycDocumentRecord> PendingKyc { get; set; } = new();
        public List<KycDocumentRecord> HistoryKyc { get; set; } = new();
    }
}
