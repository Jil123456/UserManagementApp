using UserManagementApp.Models;
using UserManagementApp.Repository;

namespace UserManagementApp.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;

        public DocumentService(IDocumentRepository repo)
        {
            _repo = repo;
        }

        public List<UserDocument> GetAllDocuments() => _repo.GetAllDocuments();
        public List<UserDocument> GetDocumentsByUserId(int userId) => _repo.GetDocumentsByUserId(userId);
        public UserDocument? GetDocumentById(int documentId) => _repo.GetDocumentById(documentId);
        public void AddDocument(UserDocument doc) => _repo.AddDocument(doc);
        public void UpdateDocumentStatus(int documentId, string status, string? actionByAdmin = null, string? rejectionReason = null)
        {
            _repo.UpdateDocumentStatus(documentId, status, actionByAdmin, rejectionReason);
        }
        public void DeleteDocument(int documentId) => _repo.DeleteDocument(documentId);
        public List<UserDocument> GetDocumentsByStatus(string status) => _repo.GetDocumentsByStatus(status);
    }
}
