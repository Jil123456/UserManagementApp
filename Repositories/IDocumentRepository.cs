using UserManagementApp.Models;

namespace UserManagementApp.Repository
{
    public interface IDocumentRepository
    {
        List<UserDocument> GetAllDocuments();
        List<UserDocument> GetDocumentsByUserId(int userId);
        UserDocument? GetDocumentById(int documentId);
        void AddDocument(UserDocument doc);
        void UpdateDocumentStatus(int documentId, string status, string? actionByAdmin = null, string? rejectionReason = null);
        void DeleteDocument(int documentId);
        List<UserDocument> GetDocumentsByStatus(string status);
    }
}
