using EduPlatform.Data.Models;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EduPlatform.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult MainPage()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>Մուտքից հետո մեկնարկային էջը՝ ըստ դերի (սովորող → իր անձնական էջ, մնացածը՝ ուսուցչի էջ)</summary>
        public IActionResult Start()
        {
            return User.IsInRole("Pupil")
                ? RedirectToAction("Index", "Pupil")
                : RedirectToAction("Index", "Teacher");
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}