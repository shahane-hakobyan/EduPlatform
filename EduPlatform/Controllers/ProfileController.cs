using EduPlatform.Resources;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// «Անձնական էջ»՝ ընդհանուր ուսուցչի և սովորողի համար (/Profile)։
    /// Ուսուցիչը տեսնում է իր պրոֆիլը և դասացուցակը, սովորողը՝ իր էջը (առայժմ՝ մշակման փուլում)։
    /// </summary>
    [Authorize(Roles = "Teacher,Pupil")]
    [Route("Profile")]
    public class ProfileController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly IScheduleService _schedule;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public ProfileController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer, IScheduleService schedule)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _schedule = schedule;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForUser(User);
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForUser(User);
            base.OnActionExecuting(context);
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            if (User.IsInRole("Pupil"))
            {
                // TODO՝ սովորողի անձնական էջը
                ViewData["Title"] = _l["PersonalPage"].Value;
                return View("_ComingSoon");
            }

            ViewBag.Schedule = _schedule.BuildReadModel(CurrentUserId, null);

            // TODO՝ տվյալները AuthServer-ից (առայժմ՝ օրինակ)
            var profile = new TeacherProfileViewModel
            {
                FullName = "Հարություն Գրիգորյան",
                Role = "Ուսուցիչ",
                PhotoUrl = "/images/avatar-placeholder.jpg",
                Email = "harutyun.grigoryan@school.am",
                PhoneNumber = "+374 77 123456",
                SchoolName = "Ստեփան Շահումյանի անվան հ. 1 հիմնական դպրոց",
                Subjects = "Մաթեմատիկա և Ֆիզիկա",
                DigitLevel = "4-րդ աստիճան",
                VoluntaryCertification = "Կամավոր ատեստավորում՝ 90-100%",
                ProfessionalAchievements = "10+ տարվա մանկավարժական փորձ, մասնակցություն մարզային մաթեմատիկայի օլիմպիադաների կազմակերպմանը, \"Լավագույն ուսուցիչ\" կոչում 2023թ.-ին, կանոնավոր վերապատրաստումների մասնակից։"
            };

            return View("Teacher", profile);
        }
    }
}
