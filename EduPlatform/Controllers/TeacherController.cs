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
                    TotalCount = 5,
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
                    Id = 2,
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
                    Id = 3,
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
                    Id = 4,
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
                    Id = 5,
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
                    TotalCount = 3,
                    Materials = new()
            {
                new MaterialCardViewModel
                {
                    Id = 6,
                    Grades = new() { "5,6,7" },
                    Subjects = new() { "Ֆիզիկա" },
                    ColorVariant = "green",
                    ThumbnailUrl = "/images/materials/1.png",
                    Authors = "Վ. Ավագյան, Ն. Մկրտչյան",
                    Publisher = "\"Աստղիկ\" հրատարակչություն",
                    Pages = 132,
                    Year = 2023,
                    ViewUrl = "https://dshh.am/am",
                    DownloadUrl = "https://dshh.am/am",
                },
                new MaterialCardViewModel
                {
                    Id = 7,
                    Grades = new() { "8,9" },
                    Subjects = new() { "Քիմիա" },
                    ColorVariant = "orange",
                    ThumbnailUrl = "/images/materials/2.png",
                    Authors = "Լ. Հարությունյան, Ռ. Գրիգորյան",
                    Publisher = "\"Զանգակ\" հրատարակչություն",
                    Pages = 176,
                    Year = 2022,
                    ViewUrl = "https://dshh.am/am",
                    DownloadUrl = "https://dshh.am/am",
                },
                new MaterialCardViewModel
                {
                    Id = 8,
                    Grades = new() { "10,11,12" },
                    Subjects = new() { "Կենսաբանություն" },
                    ColorVariant = "emerald",
                    ThumbnailUrl = "/images/materials/1.png",
                    Authors = "Ա. Պետրոսյան, Ն. Ղազարյան",
                    Publisher = "\"Աստղիկ\" հրատարակչություն",
                    Pages = 204,
                    Year = 2024,
                    ViewUrl = "https://dshh.am/am",
                    DownloadUrl = "https://dshh.am/am",
                },
            }
                },
                "Other" => new MaterialsPageViewModel
                {
                    Title = _l["OtherEducationalMaterials"],
                    Filters = OtherMaterialsFilters(),
                    TotalCount = 3,
                    Materials = new()
            {
                new MaterialCardViewModel
                {
                    Id = 9,
                    Grades = new() { "6,7" },
                    Subjects = new() { "Աշխարհագրություն" },
                    ColorVariant = "teal",
                    ThumbnailUrl = "/images/materials/2.png",
                    Authors = "Գ. Մկրտչյան, Ա. Ավագյան",
                    Publisher = "\"Զանգակ\" հրատարակչություն",
                    Pages = 48,
                    Year = 2024,
                    ViewUrl = "https://dshh.am/am",
                    DownloadUrl = "https://dshh.am/am",
                },
                new MaterialCardViewModel
                {
                    Id = 10,
                    Grades = new() { "7,8" },
                    Subjects = new() { "Պատմություն" },
                    ColorVariant = "red",
                    ThumbnailUrl = "/images/materials/1.png",
                    Authors = "Գ. Սկրտչյան, Ա. Ավագյան",
                    Publisher = "\"Աստղիկ\" հրատարակչություն",
                    Pages = 64,
                    Year = 2023,
                    ViewUrl = "https://dshh.am/am",
                    DownloadUrl = "https://dshh.am/am",
                },
                new MaterialCardViewModel
                {
                    Id = 11,
                    Grades = new() { "9,10" },
                    Subjects = new() { "Տեղեկատվական տեխնոլոգիաներ" },
                    ColorVariant = "indigo",
                    ThumbnailUrl = "/images/materials/33.png",
                    Authors = "Դ. Խաչատրյան, Ս. Ավագյան",
                    Publisher = "\"Զանգակ\" հրատարակչություն",
                    Pages = 90,
                    Year = 2024,
                    ViewUrl = "https://dshh.am/am",
                    DownloadUrl = "https://dshh.am/am",
                },
            }
                },
                _ => null
            };

            if (vm == null) return NotFound();

            return View("MaterialsList", vm);
        }

        public IActionResult Library()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var tabs = new List<LibraryTabViewModel>
    {
        new() {
            Key = "other-textbooks",
            Content = new MaterialsPageViewModel
            {
                Title = _l["OtherTextbooks"],
                Subtitle = _l["OtherTextbooksSubtitle"],
                Filters = LibraryTextbookFilters(),
                TotalCount = 3,
                Materials = new()
                {
                    new MaterialCardViewModel
                    {
                        Id = 12,
                        Grades = new() { "5,6" },
                        Subjects = new() { "Անգլերեն" },
                        ColorVariant = "pink",
                        ThumbnailUrl = "/images/materials/1.png",
                        Authors = "D. Khachatryan, S. Smith",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 180,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 13,
                        Grades = new() { "7,8" },
                        Subjects = new() { "Ռուսաց լեզու" },
                        ColorVariant = "purple",
                        ThumbnailUrl = "/images/materials/2.png",
                        Authors = "Գ. Ղազարյան, Ս. Հովհաննիսյան",
                        Publisher = "\"Աստղիկ\" հրատարակչություն",
                        Pages = 224,
                        Year = 2023,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 14,
                        Grades = new() { "9,10,11" },
                        Subjects = new() { "Ֆրանսերեն" },
                        ColorVariant = "amber",
                        ThumbnailUrl = "/images/materials/33.png",
                        Authors = "Ն. Մկրտչյան, Ա. Սարգսյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 156,
                        Year = 2024,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                }
            }
        },
        new() {
            Key = "fiction",
            Content = new MaterialsPageViewModel
            {
                Title = _l["FictionBooks"],
                Subtitle = _l["FictionBooksSubtitle"],
                Filters = LibraryFictionFilters(),
                TotalCount = 3,
                Materials = new()
                {
                    new MaterialCardViewModel
                    {
                        Id = 15,
                        Grades = new(),
                        Subjects = new(),
                        ColorVariant = "red",
                        ThumbnailUrl = "/images/materials/1.png",
                        Authors = "Հ. Թումանյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 312,
                        Year = 2020,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 16,
                        Grades = new(),
                        Subjects = new(),
                        ColorVariant = "teal",
                        ThumbnailUrl = "/images/materials/2.png",
                        Authors = "Ե. Չարենց",
                        Publisher = "\"Աստղիկ\" հրատարակչություն",
                        Pages = 198,
                        Year = 2019,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 17,
                        Grades = new(),
                        Subjects = new(),
                        ColorVariant = "indigo",
                        ThumbnailUrl = "/images/materials/33.png",
                        Authors = "Ա. Իսահակյան",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 264,
                        Year = 2021,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                }
            }
        },
        new() {
            Key = "popular-science",
            Content = new MaterialsPageViewModel
            {
                Title = _l["PopularScienceBooks"],
                Subtitle = _l["PopularScienceBooksSubtitle"],
                Filters = LibraryFictionFilters(),
                TotalCount = 2,
                Materials = new()
                {
                    new MaterialCardViewModel
                    {
                        Id = 18,
                        Grades = new(),
                        Subjects = new(),
                        ColorVariant = "green",
                        ThumbnailUrl = "/images/materials/1.png",
                        Authors = "Ս. Հոքինգ",
                        Publisher = "\"Զանգակ\" հրատարակչություն",
                        Pages = 288,
                        Year = 2018,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                    new MaterialCardViewModel
                    {
                        Id = 19,
                        Grades = new(),
                        Subjects = new(),
                        ColorVariant = "orange",
                        ThumbnailUrl = "/images/materials/2.png",
                        Authors = "Կ. Սագան",
                        Publisher = "\"Աստղիկ\" հրատարակչություն",
                        Pages = 320,
                        Year = 2017,
                        ViewUrl = "https://dshh.am/am",
                        DownloadUrl = "https://dshh.am/am",
                    },
                }
            }
        }
    };

            return View("Library", tabs);
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