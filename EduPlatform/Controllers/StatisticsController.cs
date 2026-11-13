using EduPlatform.Services;
using EduPlatform.ViewModels.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// «Իմ վիճակագրությունը»՝ ընդհանուր ուսուցչի և սովորողի համար (/Statistics)։
    /// Ցույց է տալիս օգտատիրոջ ստեղծած նյութերի դիտումները, հավանումները և տարածումները։
    /// </summary>
    [Authorize(Roles = "Teacher,Pupil")]
    [Route("Statistics")]
    public class StatisticsController : Controller
    {
        private static readonly HashSet<string> Periods = new() { "7d", "month", "3m", "year" };
        private static readonly HashSet<string> Types = new() { "", "folder", "digitized", "crossword" };

        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IMaterialStatsService _stats;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public StatisticsController(ISidebarMenuBuilder menuBuilder, IMaterialStatsService stats)
        {
            _menuBuilder = menuBuilder;
            _stats = stats;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForUser(User);
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForUser(User);
            base.OnActionExecuting(context);
        }

        // GET /Statistics?period=month&type=crossword
        [HttpGet("")]
        public async Task<IActionResult> Index(string? period, string? type)
        {
            period = period is not null && Periods.Contains(period) ? period : "month";
            type = type is not null && Types.Contains(type) ? type : "";

            var (s, failed) = await _stats.GetCombinedAsync(CurrentUserId, period, type);

            return View(new StatisticsPageViewModel
            {
                Period = period,
                Type = type,
                TotalMaterials = s.TotalMaterials,
                TotalViews = s.TotalViews,
                TotalLikes = s.TotalLikes,
                TotalShares = s.TotalShares,
                ViewsChangePercent = ChangePercent(s.TotalViews, s.PreviousPeriodViews),
                LikesChangePercent = ChangePercent(s.TotalLikes, s.PreviousPeriodLikes),
                SharesChangePercent = ChangePercent(s.TotalShares, s.PreviousPeriodShares),
                MonthlyViews = s.MonthlyViews,
                MonthlyLikes = s.MonthlyLikes,
                TopMaterials = s.TopMaterials,
                FailedSources = failed
            });
        }

        // Փոփոխության տոկոսը՝ նախորդ ժամանակաշրջանի համեմատ
        private static int ChangePercent(int current, int previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return (int)Math.Round((current - previous) / (double)previous * 100);
        }
    }
}