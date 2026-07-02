using UserManagementApp.Services;
using UserManagementApp.Constants;

namespace UserManagementApp.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Prevent infinite loop if error happens ON the error page
                if (context.Request.Path.StartsWithSegments("/Error"))
                {
                    _logger.LogError(ex, "Exception occurred on the Error page itself.");
                    throw; // Re-throw to default handler
                }

                var errorId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var method = context.Request.Method;
                var path = context.Request.Path;
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                try
                {
                    var userIdStr = context.Session.GetInt32("UserId");
                    int? userId = userIdStr.HasValue ? userIdStr.Value : null;

                    string shortMessage = $"[{errorId}] [{method}] {path} - {ex.Message}";
                    if (shortMessage.Length > 200) shortMessage = shortMessage.Substring(0, 197) + "...";

                    auditLogService.LogAction(
                        AuditActions.SYSTEM_ERROR,
                        "System",
                        "System",
                        userId,
                        $"{{\"errorId\": \"{errorId}\", \"ip\": \"{ip}\", \"message\": \"{shortMessage.Replace("\"", "\\\"")}\"}}",
                        "Error"
                    );
                }
                catch (Exception logEx)
                {
                    // Fail gracefully
                    _logger.LogError(logEx, "Failed to log system error to Audit Logs.");
                }

                // Check if AJAX/API Request
                bool isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                              context.Request.Headers["Accept"].ToString().Contains("application/json");

                if (isAjax)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"{{\"success\": false, \"message\": \"Something went wrong. Error Code: {errorId}\"}}");
                    return;
                }

                context.Response.Redirect($"/Error/500?code={errorId}");
            }
        }
    }
}
