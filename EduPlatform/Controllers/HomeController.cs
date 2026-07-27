using EduPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // The only page that doesn't require login - everything else in this
        // controller (and the app) needs a valid session.
        [AllowAnonymous]
        public IActionResult MainPage()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Placeholder for the page a user lands on after logging in.
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}