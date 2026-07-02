using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserManagementApp.Filters
{
    /// <summary>
    /// Only the Super Admin (first admin in the system) can access this action.
    /// Checks the "IsSuperAdmin" session flag set during login.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SuperAdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isSuperAdmin = context.HttpContext.Session.GetString("IsSuperAdmin");
            if (isSuperAdmin != "true")
            {
                context.HttpContext.Items["SuperAdminDenied"] = true;
                context.Result = new RedirectToActionResult("Index", "Document", new { area = "Admin" });
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
