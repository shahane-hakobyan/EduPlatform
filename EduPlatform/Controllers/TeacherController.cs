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
        public async Task<IActionResult> Repository(string id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            switch (id)
            {
                case "MyTools":
                    return View("~/Views/Teacher/Repository/MyTools.cshtml", new ToolsPageViewModel
                    {
                        Tools = new(),
                        AddToolUrl = $"{_config["Authentication:Authority"]}/Tools"
                    });

                case "Assignments":
                    return View("~/Views/Teacher/Repository/Assignments.cshtml", await BuildAssignmentsPage());

                case "AssignmentBank":
                    return View("~/Views/Teacher/Repository/AssignmentBank.cshtml");

                default:
                    return NotFound();
            }
        }

        // ── «Տրված առաջադրանքներ» ────────────────────────────────────────────

        private string AssignmentTypeLabel(AssignmentTypes type, string? otherText) => type switch
        {
            AssignmentTypes.Test => _l["AssignmentTypeTest"].Value,
            AssignmentTypes.Practical => _l["AssignmentTypePractical"].Value,
            AssignmentTypes.Laboratory => _l["AssignmentTypeLaboratory"].Value,
            AssignmentTypes.Project => _l["AssignmentTypeProject"].Value,
            AssignmentTypes.Homework => _l["AssignmentTypeHomework"].Value,
            AssignmentTypes.Other => string.IsNullOrWhiteSpace(otherText) ? _l["AssignmentTypeOther"].Value : otherText.Trim(),
            _ => ""
        };

        private string GradingTypeLabel(AssignmentGradingType type) => type switch
        {
            AssignmentGradingType.Diagnostic => _l["GradingDiagnostic"].Value,
            AssignmentGradingType.Formative => _l["GradingFormative"].Value,
            AssignmentGradingType.CurrentScore => _l["GradingCurrentScore"].Value,
            _ => ""
        };

        private async Task<AssignmentsPageViewModel> BuildAssignmentsPage()
        {
            var rows = await _db.Assignments
                .Where(a => a.TeacherUserId == CurrentTeacherUserId && !a.IsTemplate)
                .OrderBy(a => a.DueDate)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Types,
                    a.OtherTypeText,
                    a.GradingType,
                    a.StartDate,
                    a.DueDate,
                    a.IsArchived,
                    Subjects = a.Subjects.Select(s => s.Subject.Name).ToList(),
                    Levels = a.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
                    Participants = a.Recipients.Count(),
                    Completed = a.Recipients.Count(r => r.Status != AssignmentRecipientStatus.Sent),
                    Graded = a.Recipients.Count(r => r.Status == AssignmentRecipientStatus.Graded)
                })
                .ToListAsync();

            var cards = rows.Select(r =>
            {
                var typeList = Enum.GetValues<AssignmentTypes>()
                    .Where(t => t != AssignmentTypes.None && r.Types.HasFlag(t))
                    .Select(t => AssignmentTypeLabel(t, r.OtherTypeText))
                    .ToList();

                var gradeList = r.Levels.Select(l => _l["GradeLevelFormat", l].Value).ToList();

                return new AssignmentCardViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Subject = string.Join(", ", r.Subjects),
                    Grade = string.Join(", ", gradeList),
                    Type = string.Join(", ", typeList),
                    GradingType = GradingTypeLabel(r.GradingType),
                    SubjectList = r.Subjects,
                    GradeList = gradeList,
                    TypeList = typeList,
                    DueDate = r.DueDate ?? r.StartDate ?? DateTime.Today,
                    ParticipantCount = r.Participants,
                    CompletedCount = r.Completed,
                    GradedCount = r.Graded,
                    IsArchived = r.IsArchived,
                    ViewUrl = Url.Action(nameof(AssignmentDetail), new { id = r.Id })!
                };
            }).ToList();

            var filters = new List<MaterialFilterOption>
            {
                new() { Key = "subject", Label = _l["Subject"], Options = cards.SelectMany(c => c.SubjectList).Distinct().OrderBy(v => v).ToList() },
                new()
                {
                    Key = "grade", Label = _l["Grade"],
                    Options = rows.SelectMany(r => r.Levels).Distinct().OrderBy(l => l).Select(l => _l["GradeLevelFormat", l].Value).ToList()
                },
                new() { Key = "type", Label = _l["AssignmentTypeSectionLabel"], Options = cards.SelectMany(c => c.TypeList).Distinct().OrderBy(v => v).ToList() },
                new() { Key = "gradingtype", Label = _l["GradingTypeSectionLabel"], Options = cards.Select(c => c.GradingType).Distinct().OrderBy(v => v).ToList() },
                new()
                {
                    Key = "duedate", Label = _l["DueDate"],
                    Options = cards.Select(c => c.DueDate.Date).Distinct().OrderBy(d => d).Select(d => d.ToString("dd.MM.yyyy")).ToList()
                }
            };

            return new AssignmentsPageViewModel
            {
                Assignments = cards,
                Filters = filters.Where(f => f.Options.Count > 0).ToList(),
                CreateUrl = Url.Action(nameof(CreateAssignment))!
            };
        }

        public record ArchiveAssignmentRequest(int Id);

        // «Արխիվացնել»՝ ընթացիկ առաջադրանքը տեղափոխվում է «Արխիվացված»
        [HttpPost]
        public async Task<IActionResult> ArchiveAssignment([FromBody] ArchiveAssignmentRequest req)
        {
            var affected = await _db.Assignments
                .Where(a => a.Id == req.Id
                         && a.TeacherUserId == CurrentTeacherUserId
                         && !a.IsTemplate
                         && !a.IsArchived)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.IsArchived, true)
                    .SetProperty(a => a.ArchivedAt, DateTime.Now));

            return affected > 0 ? Ok() : NotFound();
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

        // ── Առաջադրանքի ստեղծում ─────────────────────────────────────────────

        private const int MaxAttachments = 10;
        private const long MaxAttachmentBytes = 25L * 1024 * 1024;

        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt",
            ".png", ".jpg", ".jpeg", ".zip"
        };

        private string AssignmentFilesRoot()
        {
            var configured = _config["AssignmentFiles:RootPath"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured;

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return Path.Combine(env.ContentRootPath, "App_Data", "assignment-files");
        }

        private async Task<CreateAssignmentViewModel> BuildCreateAssignmentViewModel(string? error = null)
        {
            var subjects = await _db.TeacherSubjects
                .Where(ts => ts.TeacherUserId == CurrentTeacherUserId)
                .Select(ts => new SelectOption { Id = ts.SubjectId, Label = ts.Subject.Name })
                .OrderBy(s => s.Label)
                .ToListAsync();

            // Մինչև AuthServer-ի հետ ինտեգրումը ուսուցչի առարկաների ցանկը կարող է դատարկ լինել՝ ցույց ենք տալիս բոլորը
            if (subjects.Count == 0)
            {
                subjects = await _db.Subjects
                    .Select(s => new SelectOption { Id = s.Id, Label = s.Name })
                    .OrderBy(s => s.Label)
                    .ToListAsync();
            }

            // Ուսուցչի դասարանները (վերջին ուսումնական տարով) և նրանց աշակերտները
            var teacherGrades = (await _db.TeacherGrades
                    .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                    .Select(tg => new { tg.GradeId, tg.Grade.GradeName, tg.AcademicYear })
                    .ToListAsync())
                .GroupBy(g => g.GradeId)
                .Select(g => g.OrderByDescending(x => x.AcademicYear).First())
                .OrderBy(g => g.GradeName)
                .ToList();

            var gradeIds = teacherGrades.Select(g => g.GradeId).ToList();
            var pupilRows = await _db.PupilGrades
                .Where(pg => gradeIds.Contains(pg.GradeId))
                .Select(pg => new { pg.PupilUserId, pg.GradeId, pg.AcademicYear })
                .ToListAsync();

            var classGroups = teacherGrades.Select(g => new AssignmentClassGroupViewModel
            {
                GradeId = g.GradeId,
                GradeName = g.GradeName,
                // Անուններն ու նկարները AuthServer-ում են՝ առայժմ տեղապահներ
                Pupils = pupilRows
                    .Where(p => p.GradeId == g.GradeId && p.AcademicYear == g.AcademicYear)
                    .OrderBy(p => p.PupilUserId)
                    .Select((p, i) => new RosterPupilViewModel
                    {
                        UserId = p.PupilUserId,
                        Name = $"{_l["Pupil"]} {i + 1}",
                        PhotoUrl = "/images/avatar-placeholder.jpg"
                    })
                    .ToList()
            }).ToList();

            return new CreateAssignmentViewModel
            {
                Subjects = subjects,
                GradeLevels = Enumerable.Range(1, 12)
                    .Select(l => new SelectOption { Id = l, Label = _l["GradeLevelFormat", l].Value })
                    .ToList(),
                AssignmentTypes = new()
                {
                    new() { Id = (int)AssignmentTypes.Test, Label = _l["AssignmentTypeTest"] },
                    new() { Id = (int)AssignmentTypes.Practical, Label = _l["AssignmentTypePractical"] },
                    new() { Id = (int)AssignmentTypes.Laboratory, Label = _l["AssignmentTypeLaboratory"] },
                    new() { Id = (int)AssignmentTypes.Project, Label = _l["AssignmentTypeProject"] },
                    new() { Id = (int)AssignmentTypes.Homework, Label = _l["AssignmentTypeHomework"] },
                    new() { Id = (int)AssignmentTypes.Other, Label = _l["AssignmentTypeOther"] }
                },
                GradingTypes = new()
                {
                    new() { Id = (int)AssignmentGradingType.Diagnostic, Label = _l["GradingDiagnostic"] },
                    new() { Id = (int)AssignmentGradingType.Formative, Label = _l["GradingFormative"] },
                    new() { Id = (int)AssignmentGradingType.CurrentScore, Label = _l["GradingCurrentScore"] }
                },
                ClassGroups = classGroups,
                SubmitUrl = Url.Action(nameof(CreateAssignment))!,
                CancelUrl = Url.Action(nameof(Repository), new { id = "Assignments" })!,
                Error = error
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreateAssignment()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            return View("~/Views/Teacher/Repository/CreateAssignment.cshtml", await BuildCreateAssignmentViewModel());
        }

        [HttpPost]
        [RequestSizeLimit(150_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 150_000_000)]
        public async Task<IActionResult> CreateAssignment(CreateAssignmentRequest req)
        {
            // ── ստուգում ─────────────────────────────────────────────────────
            var types = req.Types
                .Where(t => t != 0 && Enum.IsDefined(typeof(AssignmentTypes), t))
                .Aggregate(AssignmentTypes.None, (acc, t) => acc | (AssignmentTypes)t);

            var levels = req.GradeLevels.Where(l => l >= 1 && l <= 12).Distinct().ToList();
            var subjectIds = req.SubjectIds.Distinct().ToList();
            var pupilIds = req.PupilIds.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

            var forms = (req.AllowTextAnswer ? AssignmentResponseForms.Text : AssignmentResponseForms.None)
                      | (req.AllowFileAnswer ? AssignmentResponseForms.File : AssignmentResponseForms.None);

            var incomplete =
                string.IsNullOrWhiteSpace(req.Title)
                || subjectIds.Count == 0
                || levels.Count == 0
                || types == AssignmentTypes.None
                || (types.HasFlag(AssignmentTypes.Other) && string.IsNullOrWhiteSpace(req.OtherTypeText))
                || !Enum.IsDefined(typeof(AssignmentGradingType), req.GradingType)
                || forms == AssignmentResponseForms.None
                || pupilIds.Count == 0
                || req.DueDate <= req.StartDate;

            // ընտրված աշակերտները պետք է լինեն ՄԻԱՅՆ այս ուսուցչի դասարաններից
            var teacherGradeIds = await _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                .Select(tg => tg.GradeId)
                .ToListAsync();

            var pupilGrades = new List<(string PupilUserId, int GradeId)>();
            var validSubjectIds = new List<int>();

            if (!incomplete)
            {
                pupilGrades = (await _db.PupilGrades
                        .Where(pg => pupilIds.Contains(pg.PupilUserId) && teacherGradeIds.Contains(pg.GradeId))
                        .Select(pg => new { pg.PupilUserId, pg.GradeId })
                        .ToListAsync())
                    .GroupBy(p => p.PupilUserId)
                    .Select(g => (PupilUserId: g.Key, GradeId: g.First().GradeId))
                    .ToList();

                validSubjectIds = await _db.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToListAsync();
            }

            if (incomplete || pupilGrades.Count == 0 || validSubjectIds.Count == 0)
            {
                ViewBag.SidebarItems = _menuBuilder.ForTeacher();
                ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
                return View("~/Views/Teacher/Repository/CreateAssignment.cshtml",
                    await BuildCreateAssignmentViewModel(_l["AssignmentFormIncomplete"]));
            }

            // ── կցված ֆայլերը (սերվերի ֆայլային պահոցում) ─────────────────────
            var attachments = req.Attachments.Where(f => f.Length > 0).ToList();
            if (attachments.Count > MaxAttachments
                || attachments.Any(f => f.Length > MaxAttachmentBytes
                                        || !AllowedAttachmentExtensions.Contains(Path.GetExtension(f.FileName))))
            {
                ViewBag.SidebarItems = _menuBuilder.ForTeacher();
                ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
                return View("~/Views/Teacher/Repository/CreateAssignment.cshtml",
                    await BuildCreateAssignmentViewModel(_l["AssignmentFormIncomplete"]));
            }

            var root = AssignmentFilesRoot();
            Directory.CreateDirectory(root);

            var storedFiles = new List<(string FileName, string StoredName, string ContentType, long Size)>();
            try
            {
                foreach (var file in attachments)
                {
                    var stored = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
                    await using (var stream = System.IO.File.Create(Path.Combine(root, stored)))
                        await file.CopyToAsync(stream);

                    storedFiles.Add((Path.GetFileName(file.FileName), stored, file.ContentType, file.Length));
                }

                // ── պահպանում՝ ձևանմուշ (շտեմարան) + ուղարկված առաջադրանք ─────────
                var now = DateTime.Now;

                Assignment Make(bool isTemplate) => new()
                {
                    TeacherUserId = CurrentTeacherUserId,
                    IsTemplate = isTemplate,
                    Title = req.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                    Types = types,
                    OtherTypeText = types.HasFlag(AssignmentTypes.Other) ? req.OtherTypeText!.Trim() : null,
                    GradingType = (AssignmentGradingType)req.GradingType,
                    ResponseForms = forms,
                    StartDate = isTemplate ? null : req.StartDate,
                    DueDate = isTemplate ? null : req.DueDate,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Subjects = validSubjectIds.Select(id => new AssignmentSubject { SubjectId = id }).ToList(),
                    GradeLevels = levels.Select(l => new AssignmentGradeLevel { Level = l }).ToList(),
                    Files = storedFiles.Select(f => new AssignmentFile
                    {
                        FileName = f.FileName,
                        StoredFileName = f.StoredName,
                        ContentType = f.ContentType,
                        SizeBytes = f.Size,
                        UploadedAt = now
                    }).ToList()
                };

                var template = Make(true);
                var sent = Make(false);
                sent.Template = template;
                sent.Recipients = pupilGrades.Select(p => new AssignmentRecipient
                {
                    PupilUserId = p.PupilUserId,
                    GradeId = p.GradeId,
                    Status = AssignmentRecipientStatus.Sent,
                    SentAt = now
                }).ToList();

                _db.Assignments.AddRange(template, sent);
                await _db.SaveChangesAsync();
            }
            catch
            {
                // բազայի սխալի դեպքում ջնջում ենք արդեն գրված ֆայլերը
                foreach (var f in storedFiles)
                {
                    try { System.IO.File.Delete(Path.Combine(root, f.StoredName)); } catch { }
                }
                throw;
            }

            return RedirectToAction(nameof(Repository), new { id = "Assignments" });
        }

        [HttpGet]
        public async Task<IActionResult> AssignmentDetail(int id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var a = await _db.Assignments
                .Where(x => x.Id == id && x.TeacherUserId == CurrentTeacherUserId && !x.IsTemplate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.Types,
                    x.OtherTypeText,
                    x.GradingType,
                    x.ResponseForms,
                    x.StartDate,
                    x.DueDate,
                    x.IsArchived,
                    x.CreatedAt,
                    Subjects = x.Subjects.Select(s => s.Subject.Name).ToList(),
                    Levels = x.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
                    Files = x.Files.OrderBy(f => f.Id).Select(f => new { f.Id, f.FileName }).ToList(),
                    Recipients = x.Recipients.OrderBy(r => r.Id).Select(r => new
                    {
                        r.Id,
                        r.PupilUserId,
                        r.Status,
                        r.SubmittedAt,
                        r.AnswerText,
                        r.Score,
                        r.Feedback,
                        GradeName = r.Grade != null ? r.Grade.GradeName : "",
                        Files = r.Files.OrderBy(f => f.Id).Select(f => new { f.Id, f.FileName }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (a == null) return NotFound();

            var typeLabels = Enum.GetValues<AssignmentTypes>()
                .Where(t => t != AssignmentTypes.None && a.Types.HasFlag(t))
                .Select(t => AssignmentTypeLabel(t, a.OtherTypeText));

            var vm = new AssignmentDetailViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Subject = string.Join(", ", a.Subjects),
                Grade = string.Join(", ", a.Levels.Select(l => _l["GradeLevelFormat", l].Value)),
                Type = string.Join(", ", typeLabels),
                GradingType = GradingTypeLabel(a.GradingType),
                StartDate = a.StartDate ?? a.CreatedAt,
                DueDate = a.DueDate ?? a.StartDate ?? a.CreatedAt,
                Description = a.Description ?? "",
                IsArchived = a.IsArchived,
                AllowTextAnswer = a.ResponseForms.HasFlag(AssignmentResponseForms.Text),
                AllowFileAnswer = a.ResponseForms.HasFlag(AssignmentResponseForms.File),
                ArchiveUrl = "#",
                Files = a.Files.Select(f => new AssignmentFileViewModel
                {
                    Name = f.FileName,
                    ViewUrl = Url.Action(nameof(AssignmentFile), new { id = f.Id })!,
                    DownloadUrl = Url.Action(nameof(AssignmentFile), new { id = f.Id, download = true })!
                }).ToList(),
                Submissions = a.Recipients.Select((r, i) => new PupilSubmissionViewModel
                {
                    Id = r.Id,
                    PupilName = $"{_l["Pupil"]} {i + 1}",
                    PhotoUrl = "/images/avatar-placeholder.jpg",
                    GradeName = r.GradeName,
                    GroupName = "",
                    SubmittedDate = r.SubmittedAt,
                    Status = r.Status switch
                    {
                        AssignmentRecipientStatus.Graded => "Graded",
                        AssignmentRecipientStatus.Completed => "Submitted",
                        _ => "Pending"
                    },
                    Score = r.Score,
                    ScoreFeedback = r.Feedback ?? "",
                    AnswerText = r.AnswerText ?? "",
                    GradingType = a.GradingType.ToString(),
                    Files = r.Files.Select(f => new AssignmentFileViewModel
                    {
                        Name = f.FileName,
                        ViewUrl = Url.Action(nameof(SubmissionFile), new { id = f.Id })!,
                        DownloadUrl = Url.Action(nameof(SubmissionFile), new { id = f.Id, download = true })!
                    }).ToList()
                }).ToList()
            };

            // «Ավելացնել» մոդալը դեռ ժամանակավոր է
            var grades = await _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                .Select(tg => new SelectOption { Id = tg.GradeId, Label = tg.Grade.GradeName })
                .ToListAsync();

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

            return View(vm);
        }

        // Ուսուցչի կցած ֆայլի բացում (նոր էջում) կամ ներբեռնում
        [HttpGet]
        public async Task<IActionResult> AssignmentFile(int id, bool download = false)
        {
            var f = await _db.AssignmentFiles
                .Where(x => x.Id == id && x.Assignment.TeacherUserId == CurrentTeacherUserId)
                .Select(x => new { x.FileName, x.StoredFileName, x.ContentType })
                .FirstOrDefaultAsync();

            if (f == null) return NotFound();

            var path = Path.Combine(AssignmentFilesRoot(), Path.GetFileName(f.StoredFileName));
            if (!System.IO.File.Exists(path)) return NotFound();

            var contentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType;
            var stream = System.IO.File.OpenRead(path);

            // download=false → inline (PDF/նկար բացվում է բրաուզերում), true → ներբեռնում
            return download
                ? File(stream, contentType, f.FileName)
                : File(stream, contentType);
        }

        // Աշակերտի կցած պատասխանի ֆայլը (միայն տվյալ առաջադրանքի ուսուցչին)
        [HttpGet]
        public async Task<IActionResult> SubmissionFile(int id, bool download = false)
        {
            var f = await _db.AssignmentSubmissionFiles
                .Where(x => x.Id == id && x.Recipient.Assignment.TeacherUserId == CurrentTeacherUserId)
                .Select(x => new { x.FileName, x.StoredFileName, x.ContentType })
                .FirstOrDefaultAsync();

            if (f == null) return NotFound();

            var path = Path.Combine(AssignmentFilesRoot(), Path.GetFileName(f.StoredFileName));
            if (!System.IO.File.Exists(path)) return NotFound();

            var contentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType;
            var stream = System.IO.File.OpenRead(path);

            return download
                ? File(stream, contentType, f.FileName)
                : File(stream, contentType);
        }

        public class SaveGradeRequest
        {
            public int SubmissionId { get; set; }   // AssignmentRecipient.Id
            public decimal? Score { get; set; }
            public string? Feedback { get; set; }
        }

        // Գնահատում՝ ըստ առաջադրանքի գնահատման տեսակի
        [HttpPost]
        public async Task<IActionResult> SaveGrade([FromBody] SaveGradeRequest req)
        {
            var r = await _db.AssignmentRecipients
                .Include(x => x.Assignment)
                .FirstOrDefaultAsync(x => x.Id == req.SubmissionId
                                       && x.Assignment.TeacherUserId == CurrentTeacherUserId
                                       && !x.Assignment.IsTemplate);
            if (r == null) return NotFound();

            var feedback = string.IsNullOrWhiteSpace(req.Feedback) ? null : req.Feedback.Trim();
            if (feedback is { Length: > 2000 }) return BadRequest();

            switch (r.Assignment.GradingType)
            {
                case AssignmentGradingType.CurrentScore:
                    // միավորը (1–10) և/կամ հետադարձ կապ՝ առնվազն մեկը
                    if (req.Score is not null
                        && (req.Score < 1 || req.Score > 10 || req.Score != decimal.Truncate(req.Score.Value)))
                        return BadRequest();
                    if (req.Score is null && feedback is null) return BadRequest();
                    r.Score = req.Score;
                    break;

                case AssignmentGradingType.Diagnostic:
                    // նախնական միավոր (ազատ թիվ) և/կամ հետադարձ կապ՝ առնվազն մեկը
                    if (req.Score is not null && (req.Score < 0 || req.Score > 9999))
                        return BadRequest();
                    if (req.Score is null && feedback is null) return BadRequest();
                    r.Score = req.Score is null ? null : Math.Round(req.Score.Value, 2);
                    break;

                default: // Formative՝ միայն հետադարձ կապ
                    if (feedback is null) return BadRequest();
                    r.Score = null;
                    break;
            }

            r.Feedback = feedback;
            r.Status = AssignmentRecipientStatus.Graded;
            r.GradedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok();
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