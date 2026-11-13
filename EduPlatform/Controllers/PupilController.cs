using EduPlatform.Resources;
using EduPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// Սովորողի էջերը։ Առայժմ՝ միայն տեղապահներ, որ մենյուի հղումները աշխատեն.
    /// յուրաքանչյուր էջ կփոխարինվի իրական բովանդակությամբ, երբ պատրաստ լինի։
    /// </summary>
    [Authorize(Roles = "Pupil")]
    [Route("Pupil")]
    public class PupilController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;

        public PupilController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForPupil();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForPupil();
            base.OnActionExecuting(context);
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index() => Placeholder("PersonalPage");

        [HttpGet("Assignments")]
        public IActionResult Assignments() => Placeholder("Assignments");

        [HttpGet("Assessment/MyDiary")]
        public IActionResult MyDiary() => Placeholder("MyDiary");

        [HttpGet("Assessment/AIAssessment")]
        public IActionResult AIAssessment() => Placeholder("AIAssessment");

        [HttpGet("Messages")]
        public IActionResult Messages() => Placeholder("Messages");

        [HttpGet("AIChat")]
        public IActionResult AIChat() => Placeholder("AIChat");

        [HttpGet("MyStatistics")]
        public IActionResult MyStatistics() => Placeholder("MyStatistics");

        [HttpGet("Settings")]
        public IActionResult Settings() => Placeholder("Settings");

        // Ընդհանուր տեղապահ էջ՝ վերնագրով և «Մշակման փուլում է» տեքստով
        private IActionResult Placeholder(string titleKey)
        {
            ViewData["Title"] = _l[titleKey].Value;
            return View("Placeholder");
        }
    }
}