using EduPlatform.Data;
using EduPlatform.Helpers;
using EduPlatform.Data.Models;
using EduPlatform.Resources;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using EduPlatform.ViewModels.Library;

namespace EduPlatform.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly EduPlatformDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILibraryApiClient _library;
        private readonly IClassRosterService _roster;

        private string CurrentTeacherUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public TeacherController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer, EduPlatformDbContext db, IConfiguration config, ILibraryApiClient library, IClassRosterService roster)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
            _config = config;
            _library = library;
            _roster = roster;
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


        [HttpGet]
        public IActionResult Messages()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
            return View("~/Views/Teacher/Messages.cshtml");
        }

    }
}
