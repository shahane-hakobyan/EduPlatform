using EduPlatform.Models;
using EduPlatform.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.ViewComponents
{
    public class SidebarMenuViewComponent : ViewComponent
    {
        private readonly IIpGeolocationService _geo;

        public SidebarMenuViewComponent(IIpGeolocationService geo)
        {
            _geo = geo;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            List<SidebarMenuItem> items,
            List<SidebarMenuItem> footerItems,
            string userIp)
        {
            ViewBag.UserIp = userIp;
            ViewBag.CountryCode = await _geo.GetCountryCodeAsync(userIp);
            ViewBag.FooterItems = footerItems;
            return View(items);
        }
    }
}