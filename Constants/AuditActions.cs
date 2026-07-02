namespace UserManagementApp.Constants
{
    public static class AuditActions
    {
        public const string DOCUMENT_UPLOADED = "DOCUMENT_UPLOADED";
        public const string DOCUMENT_REUPLOADED = "DOCUMENT_REUPLOADED";
        public const string DOCUMENT_APPROVED = "DOCUMENT_APPROVED";
        public const string DOCUMENT_REJECTED = "DOCUMENT_REJECTED";
        public const string KYC_LOCKED = "KYC_LOCKED";
        
        public const string ROLE_CREATED = "ROLE_CREATED";
        public const string ROLE_UPDATED = "ROLE_UPDATED";
        public const string ROLE_DELETED = "ROLE_DELETED";
        public const string ROLE_ASSIGNED = "ROLE_ASSIGNED";
        public const string ROLE_REMOVED = "ROLE_REMOVED";
        
        public const string USER_REGISTERED = "USER_REGISTERED";
        public const string PROFILE_UPDATED = "PROFILE_UPDATED";
        public const string USER_DEACTIVATED = "USER_DEACTIVATED";
        public const string USER_REACTIVATED = "USER_REACTIVATED";
        public const string USER_DELETED = "USER_DELETED";
        
        public const string PASSWORD_RESET_REQUESTED = "PASSWORD_RESET_REQUESTED";
        public const string PASSWORD_RESET_COMPLETED = "PASSWORD_RESET_COMPLETED";
        public const string SYSTEM_ERROR = "SYSTEM_ERROR";
        
        public const string USER_LOGIN = "USER_LOGIN";
        public const string USER_LOGOUT = "USER_LOGOUT";
    }
}
