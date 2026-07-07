using EduPlatform.Models;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;

        public TeacherController(ISidebarMenuBuilder menuBuilder)
        {
            _menuBuilder = menuBuilder;
        }

        public IActionResult Index()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();

            // TODO: replace with a real call into AuthServer's profile data
            // once the login/OIDC integration is actually wired up.
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

            return View(profile);
        }
    }
}