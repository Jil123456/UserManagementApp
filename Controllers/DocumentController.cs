using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Filters;
using UserManagementApp.Models;
using UserManagementApp.Services;
using UserManagementApp.Constants;

namespace UserManagementApp.Controllers
{
    [SessionAuth]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _docService;
        private readonly IWebHostEnvironment _env;
        private readonly IAppealService _appealService;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        public DocumentController(IDocumentService docService, IWebHostEnvironment env, IAppealService appealService, IUserService userService, IAuditLogService auditLogService)
        {
            _docService = docService;
            _env = env;
            _appealService = appealService;
            _userService = userService;
            _auditLogService = auditLogService;
        }

        // GET: /Admin/Document — Admin sees ALL docs
        [AdminAuthorize]
        [Route("Admin/Document/Index")]
        public IActionResult Index()
        {
            try
            {
                var docs = _docService.GetAllDocuments()
                    .Where(d => d.DocumentType != "Aadhar Card" && d.DocumentType != "PAN Card")
                    .ToList();
                var currentRoleId = HttpContext.Session.GetInt32("RoleId");
                if (currentRoleId != 3)
                {
                    TempData["Error"] = "Access Denied: Super Admin Only.";
                    return RedirectToAction("Index", "Dashboard");
                }

                return View("~/Areas/Admin/Views/Document/Index.cshtml", docs);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(new List<UserDocument>());
            }
        }

        // GET: /Admin/Document/MyDocs — User sees their own docs
        public IActionResult MyDocs()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth", new { area = "" });

            try
            {
                var docs = _docService.GetDocumentsByUserId(userId.Value);
                return View(docs);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(new List<UserDocument>());
            }
        }

        // GET: /Admin/Document/Upload
        public IActionResult Upload()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var existingDocs = _docService.GetDocumentsByUserId(userId.Value);
            ViewBag.AadharDoc = existingDocs.FirstOrDefault(d => d.DocumentType == "Aadhar Card");
            ViewBag.PanDoc = existingDocs.FirstOrDefault(d => d.DocumentType == "PAN Card");

            return View();
        }

        // POST: /Admin/Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile AadharFile, IFormFile PanFile, IFormFile OtherFile, string OtherDocType)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var existingDocs = _docService.GetDocumentsByUserId(userId.Value);
            var existingAadhar = existingDocs.OrderByDescending(d => d.UploadedDate).FirstOrDefault(d => d.DocumentType == "Aadhar Card");
            var existingPan = existingDocs.OrderByDescending(d => d.UploadedDate).FirstOrDefault(d => d.DocumentType == "PAN Card");

            // ========== 1. LOCK — Block re-upload if Approved ==========
            if (existingAadhar != null && existingAadhar.Status == "Approved" && AadharFile != null && AadharFile.Length > 0)
            {
                TempData["Error"] = "Aadhar Card is already approved and cannot be re-uploaded \uD83D\uDD12";
                return RedirectToAction("Upload");
            }
            if (existingPan != null && existingPan.Status == "Approved" && PanFile != null && PanFile.Length > 0)
            {
                TempData["Error"] = "PAN Card is already approved and cannot be re-uploaded \uD83D\uDD12";
                return RedirectToAction("Upload");
            }

            // ========== 2. WAIT — Block re-upload if Pending (under review) ==========
            if (existingAadhar != null && existingAadhar.Status == "Pending" && AadharFile != null && AadharFile.Length > 0)
            {
                TempData["Error"] = "Aadhar Card is under review. Please wait for admin decision \u23F3";
                return RedirectToAction("Upload");
            }
            if (existingPan != null && existingPan.Status == "Pending" && PanFile != null && PanFile.Length > 0)
            {
                TempData["Error"] = "PAN Card is under review. Please wait for admin decision \u23F3";
                return RedirectToAction("Upload");
            }

            // ========== 3. COOLDOWN — Prevent spam re-uploads (2 min gap) ==========
            if (existingAadhar != null && existingAadhar.Status == "Rejected" && AadharFile != null && AadharFile.Length > 0)
            {
                if ((DateTime.UtcNow - existingAadhar.UploadedDate).TotalMinutes < 2)
                {
                    TempData["Error"] = "Please wait at least 2 minutes before re-uploading Aadhar Card \u23F3";
                    return RedirectToAction("Upload");
                }
            }
            if (existingPan != null && existingPan.Status == "Rejected" && PanFile != null && PanFile.Length > 0)
            {
                if ((DateTime.UtcNow - existingPan.UploadedDate).TotalMinutes < 2)
                {
                    TempData["Error"] = "Please wait at least 2 minutes before re-uploading PAN Card \u23F3";
                    return RedirectToAction("Upload");
                }
            }

            // ========== 4. MANDATORY CHECK — Only if not already uploaded ==========
            bool needsAadhar = existingAadhar == null || existingAadhar.Status == "Rejected";
            bool needsPan = existingPan == null || existingPan.Status == "Rejected";

            if (needsAadhar && (AadharFile == null || AadharFile.Length == 0))
            {
                TempData["Error"] = "Aadhar Card is mandatory! Please upload your Aadhar Card \u274C";
                return RedirectToAction("Upload");
            }

            if (needsPan && (PanFile == null || PanFile.Length == 0))
            {
                TempData["Error"] = "PAN Card is mandatory! Please upload your PAN Card \u274C";
                return RedirectToAction("Upload");
            }

            // ========== 5. BUILD FILE LIST ==========
            var allFiles = new List<(IFormFile file, string docType)>();

            if (AadharFile != null && AadharFile.Length > 0)
                allFiles.Add((AadharFile, "Aadhar Card"));
            
            if (PanFile != null && PanFile.Length > 0)
                allFiles.Add((PanFile, "PAN Card"));

            if (OtherFile != null && OtherFile.Length > 0)
            {
                var otherType = string.IsNullOrWhiteSpace(OtherDocType) ? "Other" : OtherDocType;

                // ========== 6. DUPLICATE CHECK — Prevent re-upload of approved Other docs ==========
                var existingOther = existingDocs.OrderByDescending(d => d.UploadedDate).FirstOrDefault(d => d.DocumentType == otherType);
                if (existingOther != null && existingOther.Status == "Approved")
                {
                    TempData["Error"] = otherType + " is already approved and cannot be uploaded again \uD83D\uDD12";
                    return RedirectToAction("Upload");
                }
                if (existingOther != null && existingOther.Status == "Pending")
                {
                    TempData["Error"] = otherType + " is under review. Please wait for admin decision \u23F3";
                    return RedirectToAction("Upload");
                }

                allFiles.Add((OtherFile, otherType));
            }

            // ========== 7. VALIDATE — Size + Extension + Magic Bytes ==========
            var allowedExtensions = new[] { ".pdf" };
            foreach (var (file, docType) in allFiles)
            {
                if (file.Length > 2 * 1024 * 1024)
                {
                    var sizeMB = (file.Length / 1024.0 / 1024.0).ToString("F1");
                    TempData["Error"] = docType + " file must be 2MB or less! Your file: " + sizeMB + "MB \u274C";
                    return RedirectToAction("Upload");
                }

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = docType + ": Only PDF files are allowed \u274C";
                    return RedirectToAction("Upload");
                }

                // Verify the magic bytes to ensure it's a 100% valid PDF
                if (!IsValidPdf(file))
                {
                    TempData["Error"] = docType + ": The file is corrupted or not a genuine PDF format \u274C";
                    return RedirectToAction("Upload");
                }
            }

            try
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                int savedCount = 0;
                foreach (var (file, docType) in allFiles)
                {
                    var uniqueName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var doc = new UserDocument
                    {
                        UserId = userId.Value,
                        DocumentType = docType,
                        FilePath = "/uploads/" + uniqueName,
                        UploadedDate = DateTime.UtcNow,
                        Status = "Pending"
                    };

                    _docService.AddDocument(doc);
                    
                    var username = HttpContext.Session.GetString("Username") ?? "Unknown";
                    string details = $"{{\"documentType\": \"{docType}\", \"fileName\": \"{file.FileName}\", \"status\": \"Pending\"}}";
                    bool isReupload = existingDocs.Any(d => d.DocumentType == docType);
                    _auditLogService.LogAction(isReupload ? AuditActions.DOCUMENT_REUPLOADED : AuditActions.DOCUMENT_UPLOADED, username, "Document", userId.Value, details, "Info");

                    savedCount++;
                }

                TempData["Success"] = savedCount + " document(s) uploaded successfully ✅";
                return RedirectToAction("MyDocs");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Upload failed: " + ex.Message;
                return View();
            }
        }

        // POST: /Admin/Document/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        [Route("Admin/Document/Approve/{id?}")]
        public IActionResult Approve(int id)
        {
            try
            {
                var doc = _docService.GetDocumentById(id);
                if (doc != null)
                {
                    // Check if it's a KYC doc
                    if (string.IsNullOrEmpty(doc.DocumentType) || doc.DocumentType == "KYC Documents" || doc.DocumentType == "Aadhar Card" || doc.DocumentType == "PAN Card")
                    {
                        var user = _userService.GetUserById(doc.UserId);
                        var adminRole = HttpContext.Session.GetInt32("RoleId");
                        if (adminRole != 3)
                        {
                            TempData["Error"] = "Only Super Admin can approve KYC documents.";
                            return RedirectToAction("Index");
                        }
                        
                        // Because it's a legacy row that holds both paths, approving this row means approving the whole KYC
                        if (string.IsNullOrEmpty(doc.DocumentType) || doc.DocumentType == "KYC Documents")
                        {
                            _userService.UpdateDocStatus(doc.UserId, "approved");
                        }
                    }

                    var currentAdmin = HttpContext.Session.GetString("Username");
                    _docService.UpdateDocumentStatus(id, "Approved", currentAdmin);
                    
                    // If it's a split KYC doc, check if BOTH are now approved
                    if (doc.DocumentType == "Aadhar Card" || doc.DocumentType == "PAN Card")
                    {
                        var allUserDocs = _docService.GetDocumentsByUserId(doc.UserId);
                        var aadhar = allUserDocs.FirstOrDefault(d => d.DocumentType == "Aadhar Card" && d.Status == "Approved");
                        var pan = allUserDocs.FirstOrDefault(d => d.DocumentType == "PAN Card" && d.Status == "Approved");
                        if (aadhar != null && pan != null)
                        {
                            _userService.UpdateDocStatus(doc.UserId, "approved");
                        }
                    }
                    
                    // Check if they had a rejected document of the same type
                    var allDocs = _docService.GetDocumentsByUserId(doc.UserId);
                    var hadRejected = allDocs.Any(d => d.DocumentType == doc.DocumentType && d.Status == "Rejected" && d.DocumentId != id);

                    if (hadRejected)
                    {
                        // Notify via chat
                        _appealService.AddAppeal(new UserAppeal {
                            UserId = doc.UserId,
                            Message = $"✅ Good news! Your updated {doc.DocumentType} has been verified and approved.",
                            SentBy = "Admin",
                            SentDate = DateTime.UtcNow
                        });

                        // Delete old rejected docs of the same type to clear the warning
                        var rejectedDocs = allDocs.Where(d => d.DocumentType == doc.DocumentType && d.Status == "Rejected" && d.DocumentId != id).ToList();
                        foreach (var r in rejectedDocs)
                        {
                            _docService.DeleteDocument(r.DocumentId);
                        }
                    }

                    TempData["Success"] = "Document approved ✅";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: /Admin/Document/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        [Route("Admin/Document/Reject/{id?}")]
        public IActionResult Reject(int id)
        {
            try
            {
                var doc = _docService.GetDocumentById(id);
                if (doc != null)
                {
                    // Check if it's a KYC doc
                    if (string.IsNullOrEmpty(doc.DocumentType) || doc.DocumentType == "KYC Documents" || doc.DocumentType == "Aadhar Card" || doc.DocumentType == "PAN Card")
                    {
                        var user = _userService.GetUserById(doc.UserId);
                        var adminRole = HttpContext.Session.GetInt32("RoleId");
                        if (adminRole != 3)
                        {
                            TempData["Error"] = "Only Super Admin can reject KYC documents.";
                            return RedirectToAction("Index");
                        }
                        
                        // Because it's a legacy row that holds both paths, rejecting this row means rejecting the whole KYC
                        if (string.IsNullOrEmpty(doc.DocumentType) || doc.DocumentType == "KYC Documents")
                        {
                            _userService.UpdateDocStatus(doc.UserId, "rejected");
                        }
                    }
                }

                var currentAdmin = HttpContext.Session.GetString("Username");
                _docService.UpdateDocumentStatus(id, "Rejected", currentAdmin);
                TempData["Success"] = "Document rejected ❌";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: /Admin/Document/Delete — Cannot delete if Approved
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        [Route("Document/Delete/{id?}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var doc = _docService.GetDocumentById(id);
                if (doc == null)
                {
                    TempData["Error"] = "Document not found ❌";
                    return RedirectToAction("Index");
                }

                if (doc.Status == "Approved")
                {
                    TempData["Error"] = "Cannot delete an approved document! Approved documents are locked 🔒";
                    return RedirectToAction("Index");
                }

                var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                _docService.DeleteDocument(id);
                TempData["Success"] = "Document deleted ✅";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Helper to check File Signature (Magic Bytes) for 100% PDF verification
        private bool IsValidPdf(IFormFile file)
        {
            if (file == null || file.Length < 5) return false;

            using (var stream = file.OpenReadStream())
            {
                byte[] buffer = new byte[5];
                stream.Read(buffer, 0, 5);
                // A valid PDF always starts with "%PDF-" (Hex: 25 50 44 46 2D)
                return buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46 && buffer[4] == 0x2D;
            }
        }
        // ✅ GET: /Admin/Document/ApprovedHistory
        [Route("Admin/Document/ApprovedHistory")]
        public IActionResult ApprovedHistory()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 3)
            {
                TempData["Error"] = "Access Denied: Super Admin Only.";
                return RedirectToAction("Index", "Dashboard");
            }
            var docs = _docService.GetDocumentsByStatus("Approved");
            return View("~/Areas/Admin/Views/Document/ApprovedHistory.cshtml", docs);
        }
    }
}


