using EduPlatform.Data;
using EduPlatform.Data.Models;
using EduPlatform.Resources;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// Ուսուցչի դասացուցակը (ցուցադրվում է անձնական էջում)՝ օրվա բովանդակություն, խմբագրում, պահպանում,
    /// և դասարանի ցուցակը։ Վերադարձնում է partial view-եր / JSON՝ teacher-schedule.js-ի համար։
    /// </summary>
    [Authorize(Roles = "Teacher")]
    [Route("Schedule")]
    public class ScheduleController : Controller
    {
        private readonly EduPlatformDbContext _db;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly IScheduleService _schedule;

        private string CurrentTeacherUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public ScheduleController(EduPlatformDbContext db, IStringLocalizer<SharedResource> localizer, IScheduleService schedule)
        {
            _db = db;
            _l = localizer;
            _schedule = schedule;
        }

        // GET /Schedule/DayContent?day=Monday
        [HttpGet("DayContent")]
        public IActionResult DayContent(string day)
        {
            return PartialView("_TeacherSchedule", _schedule.BuildReadModel(CurrentTeacherUserId, day));
        }

        // GET /Schedule/Edit?day=Monday
        [HttpGet("Edit")]
        public IActionResult Edit(string day)
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

        // POST /Schedule/Save
        [HttpPost("Save")]
        public IActionResult Save([FromBody] ScheduleSaveRequest request)
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

        // GET /Schedule/GradeRoster?gradeId=5
        [HttpGet("GradeRoster")]
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
    }
}
