using EduPlatform.Data;
using EduPlatform.Helpers;
using EduPlatform.Models;
using EduPlatform.Resources;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Controllers
{
    [AllowAnonymous]
    public class TeacherController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly EduPlatformDbContext _db;

        private const string CurrentTeacherUserId = "11111111-1111-1111-1111-111111111111"; // stand-in until real auth exists

        public TeacherController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer, EduPlatformDbContext db)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
            ViewBag.Schedule = BuildScheduleViewModel(null);

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

        [HttpGet]
        public IActionResult ScheduleDayContent(string day)
        {
            return PartialView("_TeacherSchedule", BuildScheduleViewModel(day));
        }

        [HttpGet]
        public IActionResult ScheduleEdit(string day)
        {
            var selectedDay = Enum.Parse<ScheduleDay>(day);

            var entries = _db.ScheduleLessons
                .Where(e => e.TeacherUserId == CurrentTeacherUserId && e.Day == selectedDay)
                .Include(e => e.LessonSubjects)
                .Include(e => e.LessonGrades)
                .OrderBy(e => e.PeriodNumber)
                .ToList();

            var vm = new ScheduleEditViewModel
            {
                Day = day,
                DayLabel = _l[day],
                Rows = entries.Select(e => new ScheduleEditRowViewModel
                {
                    PeriodNumber = e.PeriodNumber,
                    StartTime = e.StartTime.ToString("HH:mm"),
                    EndTime = e.EndTime.ToString("HH:mm"),
                    IsFree = e.IsFree,
                    SubjectIds = e.LessonSubjects.Select(ls => ls.SubjectId).ToList(),
                    GradeIds = e.LessonGrades.Select(lg => lg.GradeId).ToList(),
                    RoomNumber = e.RoomNumber,
                    Notes = e.Notes
                }).ToList(),
                Subjects = _db.TeacherSubjects
                    .Where(ts => ts.TeacherUserId == CurrentTeacherUserId)
                    .Select(ts => new SelectOption { Id = ts.SubjectId, Label = ts.Subject.Name })
                    .ToList(),
                Grades = _db.TeacherGrades
                    .Where(tc => tc.TeacherUserId == CurrentTeacherUserId)
                    .Select(tc => new SelectOption { Id = tc.GradeId, Label = tc.Grade.GradeName })
                    .ToList()
            };

            return PartialView("_ScheduleEditModal", vm);
        }

        public class ScheduleSaveRequest
        {
            public string Day { get; set; } = "";
            public List<ScheduleEditRowViewModel> Rows { get; set; } = new();
        }

        [HttpPost]
        public IActionResult ScheduleSave([FromBody] ScheduleSaveRequest request)
        {
            var selectedDay = Enum.Parse<ScheduleDay>(request.Day);

            var existing = _db.ScheduleLessons.Where(e => e.TeacherUserId == CurrentTeacherUserId && e.Day == selectedDay);
            _db.ScheduleLessons.RemoveRange(existing);

            foreach (var row in request.Rows)
            {
                var lesson = new ScheduleLesson
                {
                    TeacherUserId = CurrentTeacherUserId,
                    Day = selectedDay,
                    PeriodNumber = row.PeriodNumber,
                    StartTime = TimeOnly.Parse(row.StartTime!),
                    EndTime = TimeOnly.Parse(row.EndTime!),
                    IsFree = row.IsFree,
                    RoomNumber = row.IsFree ? null : row.RoomNumber,
                    Notes = row.Notes
                };
                if (!row.IsFree)
                {
                    lesson.LessonSubjects = row.SubjectIds.Select(id => new LessonSubject { SubjectId = id }).ToList();
                    lesson.LessonGrades = row.GradeIds.Select(id => new LessonGrade { GradeId = id }).ToList();
                }
                _db.ScheduleLessons.Add(lesson);
            }

            _db.SaveChanges();
            return Json(new { success = true });
        }

        private ScheduleReadViewModel BuildScheduleViewModel(string? day)
        {
            var allEntries = _db.ScheduleLessons
                  .Where(e => e.TeacherUserId == CurrentTeacherUserId)
                  .Include(e => e.LessonSubjects).ThenInclude(ls => ls.Subject)
                  .Include(e => e.LessonGrades).ThenInclude(lg => lg.Grade)
                  .ToList();

            var selectedDayValue = day ?? CurrentScheduleDay().ToString();

            var days = Enum.GetValues<ScheduleDay>()
                .Select(d => new ScheduleDayViewModel
                {
                    Day = d.ToString(),
                    DayLabel = _l[d.ToString()],
                    ClassCount = allEntries.Count(e => e.Day == d && !e.IsFree),
                    FreeCount = allEntries.Count(e => e.Day == d && e.IsFree),
                    IsSelected = d.ToString() == selectedDayValue
                })
                .ToList();

            var selectedDay = Enum.Parse<ScheduleDay>(selectedDayValue);

            return new ScheduleReadViewModel
            {
                Days = days,
                TotalClassCount = allEntries.Count(e => !e.IsFree),
                TotalFreeCount = allEntries.Count(e => e.IsFree),
                SelectedDayEntries = allEntries
                    .Where(e => e.Day == selectedDay)
                    .OrderBy(e => e.PeriodNumber)
                    .Select(e => new ScheduleEntryViewModel
                    {
                        Id = e.Id,
                        PeriodNumber = e.PeriodNumber,
                        TimeRange = $"{e.StartTime:HH:mm} – {e.EndTime:HH:mm}",
                        IsFree = e.IsFree,
                        SubjectNames = e.LessonSubjects.Select(ls => ls.Subject.Name).ToList(),
                        Grades = e.LessonGrades.Select(lg => new GradeChip { Id = lg.GradeId, Name = lg.Grade.GradeName }).ToList(),
                        RoomNumber = e.RoomNumber,
                        Notes = e.Notes
                    })
                    .ToList()
            };
        }

        [HttpGet]
        public IActionResult GradeRoster(int gradeId)
        {
            var grade = _db.Grades.FirstOrDefault(g => g.Id == gradeId);

            var vm = new GradeRosterViewModel
            {
                GradeName = grade?.GradeName ?? "",
                Pupils = Enumerable.Range(1, 8).Select(i => new RosterPupilViewModel
                {
                    Name = $"{_l["Pupil"]} {i}",
                    PhotoUrl = "/images/avatar-placeholder.jpg"
                }).ToList()
            };

            return PartialView("_GradeRoster", vm);
        }
        private static ScheduleDay CurrentScheduleDay()
        {
            var today = DateTime.Now.DayOfWeek;
            return today == DayOfWeek.Sunday ? ScheduleDay.Sunday : (ScheduleDay)(int)today;
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
                    Filters = MaterialFilters.Standard(_l),
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
                "MethodologicalGuidelines" => new MaterialsPageViewModel
                {
                    Title = _l["MethodologicalGuidelines"],
                    Filters = MaterialFilters.Standard(_l),
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
                "OtherEducationalMaterials" => new MaterialsPageViewModel
                {
                    Title = _l["OtherEducationalMaterials"],
                    Filters = MaterialFilters.Other(_l),
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

            return View("~/Views/Shared/Materials/MaterialsList.cshtml", vm);
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
                Filters = MaterialFilters.LibraryTextbook(_l),
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
                Filters = MaterialFilters.LibraryFiction(_l),
                TotalCount = 3,
                Materials = new()
                {
                    new MaterialCardViewModel
                    {
                        Id = 15,
                        Grades = new(),
                        Subjects = new(),
                         Language = "Հայերեն",
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
                         Language = "Ռուսերեն",
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
                         Language = "Անգլերեն",
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
                Filters = MaterialFilters.LibraryFiction(_l),
                TotalCount = 2,
                Materials = new()
                {
                    new MaterialCardViewModel
                    {
                        Id = 18,
                        Grades = new(),
                        Subjects = new(),
                         Language = "Հայերեն",
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
                         Language = "Անգլերեն",
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

            return View("~/Views/Shared/Materials/Library.cshtml", tabs);
        }

    }
}