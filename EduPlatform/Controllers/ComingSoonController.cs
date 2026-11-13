using EduPlatform.Resources;
using EduPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// Մենյուի այն կետերը, որոնց էջերը դեռ պատրաստ չեն (/Soon/{page})՝ «Էջը մշակման փուլում է»։
    /// Երբ էջը պատրաստ լինի, մենյուի հղումը փոխել դեպի իր կոնտրոլերը և էջը հանել այստեղից։
    /// </summary>
    [Authorize]
    [Route("Soon")]
    public class ComingSoonController : Controller
    {
        // Թույլատրված էջերը (թարգմանության բանալիներ)՝ մնացածը 404
        private static readonly HashSet<string> Pages = new(StringComparer.OrdinalIgnoreCase)
        {
            "Messages", "MyStatistics", "Settings",                       // երկուսի համար
            "MyExpertise", "MyLogbook", "AIAssessment",                    // ուսուցիչ
            "Assignments", "MyDiary", "Groups", "AIChat",                  // սովորող
        };

        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;

        public ComingSoonController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForUser(User);
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForUser(User);
            base.OnActionExecuting(context);
        }

        // GET /Soon/Messages
        [HttpGet("{page}")]
        public IActionResult Index(string page)
        {
            if (!Pages.Contains(page))
                return NotFound();

            ViewData["Title"] = _l[page].Value;
            return View("_ComingSoon");
        }
    }
}
