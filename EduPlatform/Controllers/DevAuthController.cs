using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Controllers
{

    [AllowAnonymous]
    [Route("Dev")]
    public class DevAuthController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public const string TestTeacherUserId = "11111111-1111-1111-1111-111111111111";
        public const string TestPupilUserId = "22222222-2222-2222-2222-222222222222";

        public DevAuthController(IWebHostEnvironment env) => _env = env;

        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (!_env.IsDevelopment()) return NotFound();
            return View();
        }

        [HttpPost("LoginAsTeacher")]
        public async Task<IActionResult> LoginAsTeacher()
        {
            if (!_env.IsDevelopment()) return NotFound();
            await SignInAsync(TestTeacherUserId, "Test Teacher", "Teacher");
            return RedirectToAction("Index", "Teacher");
        }

        [HttpPost("LoginAsPupil")]
        public async Task<IActionResult> LoginAsPupil()
        {
            if (!_env.IsDevelopment()) return NotFound();
            await SignInAsync(TestPupilUserId, "Test Pupil", "Pupil");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            if (!_env.IsDevelopment()) return NotFound();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignInAsync(string userId, string name, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }
    }
}