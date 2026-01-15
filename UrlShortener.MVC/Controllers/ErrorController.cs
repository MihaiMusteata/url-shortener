using Microsoft.AspNetCore.Mvc;

namespace UrlShortener.MVC.Controllers;

public class ErrorController : Controller
{
    [HttpGet("/Error/403")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("403");
    }
}