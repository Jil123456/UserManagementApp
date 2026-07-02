using Microsoft.AspNetCore.Mvc;

namespace UserManagementApp.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/500")]
        public IActionResult ServerError([FromQuery] string code)
        {
            ViewBag.ErrorCode = code;
            return View("Error500");
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    return View("Error404");
                case 403:
                    return View("Error403");
                default:
                    return View("Error500");
            }
        }
    }
}
