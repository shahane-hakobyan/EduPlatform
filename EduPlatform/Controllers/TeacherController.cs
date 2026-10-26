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

        private string CurrentTeacherUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public TeacherController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer, EduPlatformDbContext db, IConfiguration config, ILibraryApiClient library)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
            _config = config;
            _library = library;
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

        // «Իմ գրադարան» ենթամենյուի route → Library-ի MaterialKind
        private static readonly Dictionary<string, string> LibraryTypeByRoute = new()
        {
            ["Textbooks"] = "Textbook",
            ["MethodologicalGuidelines"] = "MethodicalGuide",
            ["OtherEducationalMaterials"] = "OtherMaterial",
        };

        private static readonly string[] CardColors =
            { "blue", "purple", "green", "orange", "pink", "red", "teal", "emerald", "indigo", "amber" };

        /// <summary>
        /// Ուսուցչի պահած նյութերը՝ EduPlatform-ի UserLibraryMaterials աղյուսակից,
        /// իսկ նյութերի տվյալները՝ Library.Api-ից:
        /// </summary>
        /// <summary>Library-ի OtherMaterial-ի տեսակի (Kind) անունը ընթացիկ լեզվով:</summary>
        private string? KindLabel(string? kind) => kind switch
        {
            "Text" => _l["TextMaterial"].Value,
            "Video" => _l["VideoMaterial"].Value,
            "Audio" => _l["AudioMaterial"].Value,
            "Image" => _l["ImageMaterial"].Value,
            "ThreeD" => _l["ThreeDMaterial"].Value,
            "Folder" => _l["Folder"].Value,
            "Digitized" => _l["DigitizedMaterial"].Value,
            "Crossword" => _l["Crossword"].Value,
            "PairMatch" => _l["MatchingPairsGame"].Value,
            "Learn" => _l["Quiz"].Value,
            _ => null
        };

        /// <summary>Ֆիլտրերի ընտրելի արժեքները կազմվում են էջի նյութերից:</summary>
        private List<MaterialFilterOption> BuildFilters(IReadOnlyCollection<MaterialCardViewModel> cards, params string[] keys)
        {
            var result = new List<MaterialFilterOption>();

            foreach (var key in keys)
            {
                string label;
                IEnumerable<string> values;

                switch (key)
                {
                    case "subject":
                        label = _l["Subject"]; values = cards.SelectMany(c => c.Subjects); break;
                    case "grade":
                        label = _l["Grade"]; values = cards.SelectMany(c => c.Grades); break;
                    case "author":
                        label = _l["Author"];
                        values = cards.SelectMany(c => c.Authors.Split(", ", StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "year":
                        label = _l["PublicationYear"]; values = cards.Select(c => c.Year.ToString()); break;
                    case "publisher":
                        label = _l["Publisher"]; values = cards.Select(c => c.Publisher); break;
                    case "language":
                        label = _l["Language"]; values = cards.Select(c => c.Language ?? ""); break;
                    case "type":
                        label = _l["MaterialType"]; values = cards.Select(c => c.TypeLabel ?? ""); break;
                    default:
                        continue;
                }

                var options = values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .OrderBy(v => int.TryParse(v, out var n) ? n : int.MaxValue)
                    .ThenBy(v => v)
                    .ToList();

                if (options.Count > 0)
                    result.Add(new MaterialFilterOption { Key = key, Label = label, Options = options });
            }

            return result;
        }

        private async Task<MaterialsPageViewModel> BuildSavedMaterialsPage(string route)
        {
            var type = LibraryTypeByRoute[route];

            var saved = await _db.UserLibraryMaterials
                .Where(x => x.UserId == CurrentTeacherUserId && x.MaterialType == type)
                .OrderByDescending(x => x.SavedAt)
                .ToListAsync();

            var books = await _library.GetByIdsAsync(saved.Select(s => s.LibraryMaterialId));

            if (books is not null)
            {
                // Library-ն պատասխանել է → այնտեղ ջնջված նյութերը հեռացնում ենք մեր աղյուսակից:
                var existing = books.Select(b => b.Id).ToHashSet();
                var orphans = saved.Where(s => !existing.Contains(s.LibraryMaterialId)).ToList();
                if (orphans.Count > 0)
                {
                    _db.UserLibraryMaterials.RemoveRange(orphans);
                    await _db.SaveChangesAsync();
                }
            }

            // Library-ն անհասանելի է → ոչինչ չենք հեռացնում, ցուցակը դատարկ է ցույց տրվում:
            var byId = (books ?? new()).ToDictionary(b => b.Id);

            var cards = saved
                .Where(s => byId.ContainsKey(s.LibraryMaterialId))
                .Select(s => byId[s.LibraryMaterialId])
                .Select(b => new MaterialCardViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    MaterialType = b.MaterialType,
                    TypeLabel = KindLabel(b.Kind),
                    Grades = b.Grades.OrderBy(g => g).Select(g => g.ToString()).ToList(),
                    Subjects = b.Subjects,
                    ColorVariant = CardColors[Math.Abs(b.Id) % CardColors.Length],
                    ThumbnailUrl = b.CoverUrl,
                    Authors = string.Join(", ", b.Authors),
                    Publisher = b.PublisherName,
                    Language = b.Language,
                    Pages = b.Pages ?? 0,
                    Year = b.PublishedYear,
                    ViewUrl = b.ViewUrl ?? "",
                    DownloadUrl = b.DownloadUrl ?? ""
                })
                .ToList();

            var filterKeys = route == "OtherEducationalMaterials"
                ? new[] { "subject", "grade", "author", "year", "publisher", "type" }
                : new[] { "subject", "grade", "author", "year", "publisher" };

            return new MaterialsPageViewModel
            {
                Title = _l[route],
                TotalCount = cards.Count,
                Filters = BuildFilters(cards, filterKeys),
                Materials = cards
            };
        }

        public async Task<IActionResult> Materials(string id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            if (LibraryTypeByRoute.ContainsKey(id))
            {
                var page = await BuildSavedMaterialsPage(id);
                return View("~/Views/Shared/Materials/MaterialsList.cshtml", page);
            }

            return NotFound();
        }

        public record RemoveSavedRequest(string MaterialType, int Id);

        // «Իմ գրադարան»-ից նյութի հեռացում (Library-ի նյութը չի ջնջվում, միայն այս օգտատիրոջ պահվածը)
        [HttpPost]
        public async Task<IActionResult> RemoveSavedMaterial([FromBody] RemoveSavedRequest req)
        {
            await _db.UserLibraryMaterials
                .Where(x => x.UserId == CurrentTeacherUserId
                         && x.MaterialType == req.MaterialType
                         && x.LibraryMaterialId == req.Id)
                .ExecuteDeleteAsync();

            return Ok();
        }

        [HttpGet]
        public IActionResult Repository(string id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            return id switch
            {
                "MyTools" => View("~/Views/Teacher/Repository/MyTools.cshtml", new ToolsPageViewModel
                {
                    Tools = new(),
                    AddToolUrl = $"{_config["Authentication:Authority"]}/Tools"
                }),
                "Assignments" => View("~/Views/Teacher/Repository/Assignments.cshtml", new AssignmentsPageViewModel
                {
                    Assignments = new(),
                    CreateUrl = Url.Action(nameof(CreateAssignment))!
                }),
                "AssignmentBank" => View("~/Views/Teacher/Repository/AssignmentBank.cshtml"),
                _ => NotFound()
            };
        }

        [HttpGet]
        public IActionResult Messages()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
            return View("~/Views/Teacher/Messages.cshtml");
        }

        public async Task<IActionResult> OtherBooks()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var saved = await _db.UserLibraryMaterials
                .Where(x => x.UserId == CurrentTeacherUserId && x.MaterialType == "OtherBook")
                .OrderByDescending(x => x.SavedAt)
                .ToListAsync();

            var books = await _library.GetByIdsAsync(saved.Select(s => s.LibraryMaterialId));

            if (books is not null)
            {
                // Library-ում ջնջված նյութերը հեռացնում ենք մեր աղյուսակից
                var existing = books.Select(b => b.Id).ToHashSet();
                var orphans = saved.Where(s => !existing.Contains(s.LibraryMaterialId)).ToList();
                if (orphans.Count > 0)
                {
                    _db.UserLibraryMaterials.RemoveRange(orphans);
                    await _db.SaveChangesAsync();
                }
            }

            // Library-ն անհասանելի է → ոչինչ չենք հեռացնում, ցուցակը դատարկ է
            var byId = (books ?? new()).ToDictionary(b => b.Id);
            var ordered = saved
                .Where(s => byId.ContainsKey(s.LibraryMaterialId))
                .Select(s => byId[s.LibraryMaterialId])
                .ToList();

            MaterialsPageViewModel TabContent(string category, string title, params string[] filterKeys)
            {
                var cards = ordered
                    .Where(b => b.Category == category)
                    .Select(b => new MaterialCardViewModel
                    {
                        Id = b.Id,
                        Title = b.Title,
                        MaterialType = b.MaterialType,
                        Grades = b.Grades.OrderBy(g => g).Select(g => g.ToString()).ToList(),
                        Subjects = b.Subjects,
                        ColorVariant = CardColors[Math.Abs(b.Id) % CardColors.Length],
                        ThumbnailUrl = b.CoverUrl,
                        Authors = string.Join(", ", b.Authors),
                        Publisher = b.PublisherName,
                        Language = b.Language,
                        Pages = b.Pages ?? 0,
                        Year = b.PublishedYear,
                        ViewUrl = b.ViewUrl ?? "",
                        DownloadUrl = b.DownloadUrl ?? ""
                    })
                    .ToList();

                return new MaterialsPageViewModel { Title = title, TotalCount = cards.Count, Filters = BuildFilters(cards, filterKeys), Materials = cards };
            }

            var tabs = new List<OtherBooksTabViewModel>
            {
                new() { Key = "other-textbooks", Content = TabContent("Textbook", _l["OtherTextbooks"], "subject", "grade", "author", "year", "publisher", "language") },
                new() { Key = "fiction", Content = TabContent("Fiction", _l["FictionBooks"], "author", "year", "publisher", "language") },
                new() { Key = "popular-science", Content = TabContent("PopularScience", _l["PopularScienceBooks"], "author", "year", "publisher", "language") },
            };

            return View("~/Views/Shared/Materials/OtherBooks.cshtml", tabs);
        }

        [HttpGet]
        public IActionResult CreateAssignment()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var grades = _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                .Select(tg => new SelectOption { Id = tg.GradeId, Label = tg.Grade.GradeName })
                .ToList();

            var vm = new CreateAssignmentViewModel
            {
                Subjects = _db.TeacherSubjects
                    .Where(ts => ts.TeacherUserId == CurrentTeacherUserId)
                    .Select(ts => new SelectOption { Id = ts.SubjectId, Label = ts.Subject.Name })
                    .ToList(),
                Grades = grades,
                AssignmentTypes = new() { "Թեստային", "Գործնական", "Լաբորատոր", "Նախագծային", "Տնային Աշխատանք", "Այլ" },
                GradingTypes = new() { "Հայտորոշիչ", "Ձևավորող", "Ամփոփիչ" },
                ClassGroups = grades.Select(g => new AssignmentClassGroupViewModel
                {
                    GradeId = g.Id,
                    GradeName = g.Label,
                    Pupils = Enumerable.Range(1, 8).Select(i => new RosterPupilViewModel
                    {
                        Name = $"{_l["Pupil"]} {i}",
                        PhotoUrl = "/images/avatar-placeholder.jpg"
                    }).ToList()
                }).ToList(),
                SubmitUrl = Url.Action(nameof(CreateAssignment))!,
                CancelUrl = Url.Action(nameof(Repository), new { id = "Assignments" })!
            };

            return View("~/Views/Teacher/Repository/CreateAssignment.cshtml", vm);
        }

        [HttpPost]
        public IActionResult CreateAssignment(CreateAssignmentViewModel _)
        {
            // No Assignment entity/table exists yet — this just returns to the list.
            // Wire this up to a real save once the Assignment model + migration are added.
            return RedirectToAction(nameof(Repository), new { id = "Assignments" });
        }

        [HttpGet]
        public IActionResult AssignmentDetail(int id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var vm = new AssignmentDetailViewModel
            {
                Id = id,
                Title = "Ծաղիկի կառուցվածք",
                Subject = "Կենսաբանություն",
                Grade = "9-րդ դասարան",
                Type = "Գործնական",
                GradingType = "Ձևավորող",
                StartDate = new DateTime(2028, 3, 8),
                DueDate = new DateTime(2028, 3, 15),
                Description = "պատասխան ",
                ArchiveUrl = "#",
                Submissions = new()
        {
            new() {
                Id = 1, PupilName = "Լուսինե Գրիգորյան", PhotoUrl = "/images/avatar-placeholder.jpg",
                GradeName = "9-րդ դասարան", GroupName = "Խումբ Ա",
                SubmittedDate = new DateTime(2028, 3, 10), Status = "Graded",
                Score = 7, MaxScore = 10, ScoreFeedback = "Կարելի է ավելի լավ",
                AttachmentFileName = "Լուսինե Գրիգորյան.pdf", AttachmentUrl = "#",
                GradingMode = "Point"
            },
            new() {
                Id = 2, PupilName = "Լիլիթ Բադոյան", PhotoUrl = "/images/avatar-placeholder.jpg",
                GradeName = "9-րդ դասարան", GroupName = "Խումբ Ա",
                SubmittedDate = new DateTime(2028, 3, 11), Status = "Submitted",
                AttachmentFileName = "Լիլիթ Բադոյան.pdf", AttachmentUrl = "#",
                GradingMode = "Formative"
            },
        }
            };

            var grades = _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                .Select(tg => new SelectOption { Id = tg.GradeId, Label = tg.Grade.GradeName })
                .ToList();

            vm.AvailableGrades = grades.Select(g => new AssignmentClassGroupViewModel
            {
                GradeId = g.Id,
                GradeName = g.Label,
                Pupils = Enumerable.Range(1, 8).Select(i => new RosterPupilViewModel
                {
                    Name = $"{_l["Pupil"]} {i}",
                    PhotoUrl = "/images/avatar-placeholder.jpg"
                }).ToList()
            }).ToList();

            vm.AlreadyAddedPupilIds = vm.Submissions.Select(s => s.PupilName.GetHashCode()).ToList();

            return View(vm);
        }

        public class SaveGradeRequest
        {
            public int SubmissionId { get; set; }
            public string GradingMode { get; set; } = "Point";
            public int? Score { get; set; }
            public string? ProtocolText { get; set; }
        }

        [HttpPost]
        public IActionResult SaveGrade([FromBody] SaveGradeRequest request)
        {
            // No submission/grade persistence yet — wire this to a real
            // Submission entity once it exists in the DB.
            return Json(new { success = true });
        }
        public class AddPupilsRequest
        {
            public int AssignmentId { get; set; }
            public List<string> PupilNames { get; set; } = new();
        }

        [HttpPost]
        public IActionResult AddPupilsToAssignment([FromBody] AddPupilsRequest request)
        {
            // No Submission entity yet — wire this to a real insert once it exists.
            return Json(new { success = true, added = request.PupilNames.Count });
        }

    }
}