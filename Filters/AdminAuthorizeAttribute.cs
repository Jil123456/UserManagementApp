using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserManagementApp.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var roleId = context.HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1 && roleId != 3) // 1 = Admin, 3 = Super Admin
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}
