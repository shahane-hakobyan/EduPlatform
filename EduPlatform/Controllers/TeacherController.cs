using EduPlatform.Models;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using EduPlatform.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;

        public TeacherController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
        }

        public IActionResult Index()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

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

        // GET /Teacher/Materials/Textbooks | /Teacher/Materials/Guides | /Teacher/Materials/Other
        // "id" here is the sub-page name — matches the default {controller}/{action}/{id?}
        // route exactly against the URLs already registered in SidebarMenuBuilder.
        public IActionResult Materials(string id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            MaterialsPageViewModel? vm = id switch
            {
                "Textbooks" => new MaterialsPageViewModel
                {
                    Title = _l["Textbooks"],
                    Filters = StandardMaterialFilters(),
                    TotalCount = 1,
                    Materials = new()
                {
                    new MaterialCardViewModel
                    {
                        Id = 1,
                        Grades = new() { "7,8,9" },
                        Subjects = new() { "Մաթեմատիկա, Ռուսաց լեզու, Գրականություն, Անգլերեն" },
                        ColorVariant = "blue",
                        ThumbnailUrl = "/images/materials/1.png",
                        Authors = "Ա. Սարգսյան, Մ. Պետրոսյան, Ա․ Իշխանյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 248,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 1,
                        Grades = new() { "7,8,9" },
                        Subjects = new() { "Մաթեմատիկա, Ռուսաց լեզու, Գրականություն, Անգլերեն" },
                        ColorVariant = "blue",
                         ThumbnailUrl = "/images/materials/2.png",
                        Authors = "Ա. Սարգսյան, Մ. Պետրոսյան, Ա․ Իշխանյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 248,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 1,
                        Grades = new() { "7,8,9" },
                        Subjects = new() { "Մաթեմատիկա, Ռուսաց լեզու, Գրականություն, Անգլերեն" },
                        ColorVariant = "blue",
                        ThumbnailUrl = "/images/materials/1.png",
                        Authors = "Ա. Սարգսյան, Մ. Պետրոսյան, Ա․ Իշխանյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 248,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 1,
                        Grades = new() { "7,8,9" },
                        Subjects = new() { "Մաթեմատիկա, Ռուսաց լեզու, Գրականություն, Անգլերեն" },
                        ColorVariant = "blue",
                        ThumbnailUrl = "/images/materials/1.png",
                        Authors = "Ա. Սարգսյան, Մ. Պետրոսյան, Ա․ Իշխանյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 248,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 1,
                        Grades = new() { "7,8,9" },
                        Subjects = new() { "Մաթեմատիկա, Ռուսաց լեզու, Գրականություն, Անգլերեն" },
                        ColorVariant = "blue",
                        ThumbnailUrl = "/images/materials/33.png",
                        Authors = "Ա. Սարգսյան, Մ. Պետրոսյան, Ա․ Իշխանյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 248,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },

                }
                },
                "Guides" => new MaterialsPageViewModel
                {
                    Title = _l["MethodologicalGuidelines"],
                    Filters = StandardMaterialFilters(),
                    TotalCount = 0,
                    Materials = new()
                },
                "Other" => new MaterialsPageViewModel
                {
                    Title = _l["OtherEducationalMaterials"],
                    Filters = OtherMaterialsFilters(),
                    TotalCount = 0,
                    Materials = new()
                },
                _ => null
            };

            if (vm == null) return NotFound();

            return View("MaterialsList", vm);
        }

        // GET /Teacher/Library
        public IActionResult Library()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var vm = new MaterialsPageViewModel
            {
                Title = _l["Library"],
                Filters = LibraryTextbookFilters(),
                TotalCount = 0,
                Materials = new()
            };
            return View("MaterialsList", vm);
        }

        private List<MaterialFilterOption> StandardMaterialFilters() => new() {
            new() { Key = "subject", Label = _l["Subject"] },
            new() { Key = "grade", Label = _l["Grade"] },
            new() { Key = "author", Label = _l["Author"] },
            new() { Key = "year", Label = _l["PublicationYear"] },
            new() { Key = "publisher", Label = _l["Publisher"] },
        };

        private List<MaterialFilterOption> OtherMaterialsFilters() =>
            StandardMaterialFilters().Append(new() { Key = "type", Label = _l["MaterialType"] }).ToList();

        private List<MaterialFilterOption> LibraryTextbookFilters() =>
            StandardMaterialFilters().Append(new() { Key = "language", Label = _l["Language"] }).ToList();

        private List<MaterialFilterOption> LibraryFictionFilters() => new() {
            new() { Key = "author", Label = _l["Author"] },
            new() { Key = "year", Label = _l["PublicationYear"] },
            new() { Key = "publisher", Label = _l["Publisher"] },
            new() { Key = "language", Label = _l["Language"] },
        };
    }
}