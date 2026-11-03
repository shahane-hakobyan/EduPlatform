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
                    return View("~/Views/Teacher/Repository/AssignmentBank.cshtml", await BuildAssignmentBank());

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

        // ── «Առաջադրանքների շտեմարան» ────────────────────────────────────────

        private async Task<AssignmentBankViewModel> BuildAssignmentBank()
        {
            var rows = await _db.Assignments
                .Where(a => a.TeacherUserId == CurrentTeacherUserId && a.IsTemplate)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Types,
                    a.OtherTypeText,
                    a.GradingType,
                    a.CreatedAt,
                    Subjects = a.Subjects.Select(s => s.Subject.Name).ToList(),
                    Levels = a.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
                    FilesCount = a.Files.Count(),
                    SentCount = _db.Assignments.Count(x => x.TemplateId == a.Id)
                })
                .ToListAsync();

            var cards = rows.Select(r =>
            {
                var typeList = Enum.GetValues<AssignmentTypes>()
                    .Where(t => t != AssignmentTypes.None && r.Types.HasFlag(t))
                    .Select(t => AssignmentTypeLabel(t, r.OtherTypeText))
                    .ToList();

                var gradeList = r.Levels.Select(l => _l["GradeLevelFormat", l].Value).ToList();

                return new AssignmentBankCardViewModel
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
                    CreatedAt = r.CreatedAt,
                    FilesCount = r.FilesCount,
                    SentCount = r.SentCount
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
                    Key = "created", Label = _l["CreatedDate"],
                    Options = cards.Select(c => c.CreatedAt.Date).Distinct().OrderByDescending(d => d).Select(d => d.ToString("dd.MM.yyyy")).ToList()
                }
            };

            return new AssignmentBankViewModel
            {
                Items = cards,
                Filters = filters.Where(f => f.Options.Count > 0).ToList(),
                CreateUrl = Url.Action(nameof(CreateAssignment))!
            };
        }

        // ── Շտեմարանի գրառման դիտում / խմբագրում ──────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ViewBankAssignment(int id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var t = await _db.Assignments
                .Where(x => x.Id == id && x.TeacherUserId == CurrentTeacherUserId && x.IsTemplate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.Types,
                    x.OtherTypeText,
                    x.GradingType,
                    x.ResponseForms,
                    Subjects = x.Subjects.Select(s => s.Subject.Name).ToList(),
                    Levels = x.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
                    Files = x.Files.OrderBy(f => f.Id).Select(f => new { f.Id, f.FileName }).ToList()
                })
                .FirstOrDefaultAsync();
            if (t == null) return NotFound();

            var vm = new AssignmentDetailViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Subject = string.Join(", ", t.Subjects),
                Grade = string.Join(", ", t.Levels.Select(l => _l["GradeLevelFormat", l].Value)),
                Type = string.Join(", ", Enum.GetValues<AssignmentTypes>()
                    .Where(f => f != AssignmentTypes.None && t.Types.HasFlag(f))
                    .Select(f => AssignmentTypeLabel(f, t.OtherTypeText))),
                GradingType = GradingTypeLabel(t.GradingType),
                Description = t.Description ?? "",
                AllowTextAnswer = t.ResponseForms.HasFlag(AssignmentResponseForms.Text),
                AllowFileAnswer = t.ResponseForms.HasFlag(AssignmentResponseForms.File),
                Files = t.Files.Select(f => new AssignmentFileViewModel
                {
                    Name = f.FileName,
                    ViewUrl = Url.Action(nameof(AssignmentFile), new { id = f.Id })!,
                    DownloadUrl = Url.Action(nameof(AssignmentFile), new { id = f.Id, download = true })!
                }).ToList()
            };

            ViewBag.SendUrl = Url.Action(nameof(CreateAssignment), new { fromTemplate = id });
            ViewBag.EditUrl = Url.Action(nameof(EditBankAssignment), new { id });
            ViewBag.BackUrl = Url.Action(nameof(Repository), new { id = "AssignmentBank" });

            return View("~/Views/Teacher/Repository/AssignmentBankView.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditBankAssignment(int id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var vm = await BuildCreateAssignmentViewModel();
            vm.Prefill = await LoadAssignmentPrefill(id);
            if (vm.Prefill == null) return NotFound();

            vm.Mode = "Edit";
            vm.SubmitUrl = Url.Action(nameof(EditBankAssignment), new { id })!;
            vm.CancelUrl = Url.Action(nameof(Repository), new { id = "AssignmentBank" })!;

            return View("~/Views/Teacher/Repository/CreateAssignment.cshtml", vm);
        }

        [HttpPost]
        [RequestSizeLimit(150_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 150_000_000)]
        public async Task<IActionResult> EditBankAssignment(int id, CreateAssignmentRequest req)
        {
            var a = await _db.Assignments
                .Include(x => x.Subjects)
                .Include(x => x.GradeLevels)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id && x.TeacherUserId == CurrentTeacherUserId && x.IsTemplate);
            if (a == null) return NotFound();

            var types = req.Types
                .Where(t => t != 0 && Enum.IsDefined(typeof(AssignmentTypes), t))
                .Aggregate(AssignmentTypes.None, (acc, t) => acc | (AssignmentTypes)t);
            var levels = req.GradeLevels.Where(l => l >= 1 && l <= 12).Distinct().ToList();
            var subjectIds = req.SubjectIds.Distinct().ToList();
            var forms = (req.AllowTextAnswer ? AssignmentResponseForms.Text : AssignmentResponseForms.None)
                      | (req.AllowFileAnswer ? AssignmentResponseForms.File : AssignmentResponseForms.None);

            var validSubjectIds = subjectIds.Count == 0
                ? new List<int>()
                : await _db.Subjects.Where(s => subjectIds.Contains(s.Id)).Select(s => s.Id).ToListAsync();

            var attachments = req.Attachments.Where(f => f.Length > 0).ToList();
            var keepIds = req.KeepFileIds.ToHashSet();
            var keptCount = a.Files.Count(f => keepIds.Contains(f.Id));

            var invalid =
                string.IsNullOrWhiteSpace(req.Title)
                || validSubjectIds.Count == 0
                || levels.Count == 0
                || types == AssignmentTypes.None
                || (types.HasFlag(AssignmentTypes.Other) && string.IsNullOrWhiteSpace(req.OtherTypeText))
                || !Enum.IsDefined(typeof(AssignmentGradingType), req.GradingType)
                || forms == AssignmentResponseForms.None
                || attachments.Count + keptCount > MaxAttachments
                || attachments.Any(f => f.Length > MaxAttachmentBytes
                                        || !AllowedAttachmentExtensions.Contains(Path.GetExtension(f.FileName)));

            if (invalid)
            {
                ViewBag.SidebarItems = _menuBuilder.ForTeacher();
                ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

                req.SourceTemplateId = id;
                var vm = await BuildCreateAssignmentViewModelFromRequest(req, _l["AssignmentFormIncomplete"].Value);
                vm.Mode = "Edit";
                vm.SubmitUrl = Url.Action(nameof(EditBankAssignment), new { id })!;
                vm.CancelUrl = Url.Action(nameof(Repository), new { id = "AssignmentBank" })!;
                return View("~/Views/Teacher/Repository/CreateAssignment.cshtml", vm);
            }

            var root = AssignmentFilesRoot();
            Directory.CreateDirectory(root);
            var now = DateTime.Now;
            var storedNames = new List<string>();

            try
            {
                var newFiles = new List<AssignmentFile>();
                foreach (var file in attachments)
                {
                    var stored = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
                    await using (var stream = System.IO.File.Create(Path.Combine(root, stored)))
                        await file.CopyToAsync(stream);
                    storedNames.Add(stored);

                    newFiles.Add(new AssignmentFile
                    {
                        FileName = Path.GetFileName(file.FileName),
                        StoredFileName = stored,
                        ContentType = file.ContentType,
                        SizeBytes = file.Length,
                        UploadedAt = now
                    });
                }

                a.Title = req.Title.Trim();
                a.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
                a.Types = types;
                a.OtherTypeText = types.HasFlag(AssignmentTypes.Other) ? req.OtherTypeText!.Trim() : null;
                a.GradingType = (AssignmentGradingType)req.GradingType;
                a.ResponseForms = forms;
                a.UpdatedAt = now;

                // առարկաներ՝ ջնջում ենք հանվածները, ավելացնում նորերը
                _db.AssignmentSubjects.RemoveRange(a.Subjects.Where(s => !validSubjectIds.Contains(s.SubjectId)));
                foreach (var sid in validSubjectIds.Where(sid => a.Subjects.All(s => s.SubjectId != sid)))
                    a.Subjects.Add(new AssignmentSubject { SubjectId = sid });

                // դասարանների մակարդակներ
                _db.AssignmentGradeLevels.RemoveRange(a.GradeLevels.Where(g => !levels.Contains(g.Level)));
                foreach (var l in levels.Where(l => a.GradeLevels.All(g => g.Level != l)))
                    a.GradeLevels.Add(new AssignmentGradeLevel { Level = l });

                // ֆայլեր՝ հանվածների գրառումները ջնջվում են (սկավառակի ֆայլը մնում է, քանի որ ուղարկվածները կարող են օգտագործել)
                _db.AssignmentFiles.RemoveRange(a.Files.Where(f => !keepIds.Contains(f.Id)));
                foreach (var f in newFiles) a.Files.Add(f);

                await _db.SaveChangesAsync();
            }
            catch
            {
                foreach (var n in storedNames)
                {
                    try { System.IO.File.Delete(Path.Combine(root, n)); } catch { }
                }
                throw;
            }

            return RedirectToAction(nameof(Repository), new { id = "AssignmentBank" });
        }

        public record DeleteBankAssignmentRequest(int Id);

        // Շտեմարանից ջնջում՝ միայն շտեմարանի գրառումը, արդեն ուղարկվածները (աշակերտների աշխատանքով) մնում են
        [HttpPost]
        public async Task<IActionResult> DeleteBankAssignment([FromBody] DeleteBankAssignmentRequest req)
        {
            var exists = await _db.Assignments
                .AnyAsync(a => a.Id == req.Id && a.TeacherUserId == CurrentTeacherUserId && a.IsTemplate);
            if (!exists) return NotFound();

            await using var tx = await _db.Database.BeginTransactionAsync();

            await _db.Assignments
                .Where(a => a.TemplateId == req.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TemplateId, (int?)null));

            await _db.Assignments
                .Where(a => a.Id == req.Id && a.TeacherUserId == CurrentTeacherUserId && a.IsTemplate)
                .ExecuteDeleteAsync();

            await tx.CommitAsync();
            return Ok();
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

        // ── «Խմբեր» ───────────────────────────────────────────────────────────

        private string GroupTypeLabel(GroupType type) => type switch
        {
            GroupType.Pupils => _l["GroupTypePupils"].Value,
            GroupType.Parents => _l["GroupTypeParents"].Value,
            GroupType.Other => _l["GroupTypeOther"].Value,
            _ => ""
        };

        private string GroupImagesRoot()
        {
            var configured = _config["GroupImages:RootPath"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured;

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return Path.Combine(env.ContentRootPath, "App_Data", "group-images");
        }

        // «Իմ խմբերը»՝ այն խմբերը, որոնց ադմինն եմ ես, և այն խմբերը, որոնցում ինձ ավելացրել են
        [HttpGet]
        public async Task<IActionResult> Groups()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var rows = await _db.UserGroupMembers
                .Where(m => m.UserId == CurrentTeacherUserId)
                .OrderByDescending(m => m.Group.CreatedAt)
                .Select(m => new
                {
                    m.Group.Id,
                    m.Group.Name,
                    m.Group.Type,
                    m.Group.ImageStoredFileName,
                    m.Role,
                    MemberCount = m.Group.Members.Count()
                })
                .ToListAsync();

            var cards = rows.Select(r => new GroupCardViewModel
            {
                Id = r.Id,
                Name = r.Name,
                ImageUrl = r.ImageStoredFileName == null ? null : Url.Action(nameof(GroupImage), new { id = r.Id }),
                TypeKey = r.Type.ToString(),
                TypeLabel = GroupTypeLabel(r.Type),
                IsAdmin = r.Role == GroupMemberRole.Admin,
                RoleLabel = r.Role == GroupMemberRole.Admin ? _l["GroupRoleAdmin"].Value : _l["GroupRoleMember"].Value,
                RoleShortLabel = r.Role == GroupMemberRole.Admin ? _l["GroupRoleAdminShort"].Value : _l["GroupRoleMemberShort"].Value,
                MemberCount = r.MemberCount,
                DetailUrl = Url.Action(nameof(GroupDetail), new { id = r.Id })!
            }).ToList();

            var filters = new List<MaterialFilterOption>
            {
                new()
                {
                    Key = "type", Label = _l["GroupTypeLabel"],
                    Options = new() { GroupTypeLabel(GroupType.Pupils), GroupTypeLabel(GroupType.Parents), GroupTypeLabel(GroupType.Other) }
                },
                new()
                {
                    Key = "role", Label = _l["MyRoleInGroup"],
                    Options = new() { _l["GroupRoleAdmin"].Value, _l["GroupRoleMember"].Value }
                }
            };

            return View("~/Views/Teacher/Groups.cshtml", new GroupsPageViewModel
            {
                Groups = cards,
                Filters = filters,
                CreateUrl = "/Teacher/CreateGroup"
            });
        }

        private const long MaxGroupImageBytes = 5 * 1024 * 1024;
        private const string ParentIdPrefix = "parent:";

        // JPG/PNG՝ ըստ ընդլայնման, MIME տեսակի և ֆայլի առաջին բայթերի
        private static bool IsValidGroupImage(Microsoft.AspNetCore.Http.IFormFile image)
        {
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png")) return false;
            if (image.Length == 0 || image.Length > MaxGroupImageBytes) return false;
            if (image.ContentType is not ("image/jpeg" or "image/png")) return false;

            Span<byte> head = stackalloc byte[4];
            using var s = image.OpenReadStream();
            if (s.Read(head) < 4) return false;

            var isJpeg = head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;
            var isPng = head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47;
            return isJpeg || isPng;
        }

        // Անդամների Id-ների ստուգում՝ սովորողների/ծնողների խմբում թույլատրվում են միայն ուսուցչի դասարանների սովորողները
        private async Task<List<string>> ResolveGroupMemberIds(GroupType type, IEnumerable<string> ids)
        {
            if (type != GroupType.Pupils && type != GroupType.Parents) return new List<string>();

            var wanted = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (wanted.Count == 0) return new List<string>();

            // Ծնողի UserId-ն առայժմ ժամանակավոր է՝ «parent:{սովորողի UserId}»։
            // AuthServer-ից իրական ծնողներ ստանալուց հետո այստեղ կդրվի ծնողի իրական UserId-ն։
            var pupilIdsWanted = type == GroupType.Parents
                ? wanted.Where(x => x.StartsWith(ParentIdPrefix)).Select(x => x[ParentIdPrefix.Length..]).ToList()
                : wanted;

            var teacherGradeIds = await _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                .Select(tg => tg.GradeId)
                .ToListAsync();

            var validPupilIds = await _db.PupilGrades
                .Where(pg => pupilIdsWanted.Contains(pg.PupilUserId) && teacherGradeIds.Contains(pg.GradeId))
                .Select(pg => pg.PupilUserId)
                .Distinct()
                .ToListAsync();

            return type == GroupType.Parents
                ? validPupilIds.Select(p => ParentIdPrefix + p).ToList()
                : validPupilIds;
        }

        // Անդամի ցուցադրվող անունը (AuthServer-ի անունները առայժմ տեղապահներ են)
        private async Task<Func<string, int, string>> GroupMemberNamer()
        {
            var pupilNames = (await BuildClassGroups())
                .SelectMany(cg => cg.Pupils)
                .GroupBy(p => p.UserId)
                .ToDictionary(p => p.Key, p => p.First().Name);

            return (userId, index) =>
            {
                if (userId == CurrentTeacherUserId) return _l["YouLabel"].Value;

                if (userId.StartsWith(ParentIdPrefix))
                {
                    var pupilId = userId[ParentIdPrefix.Length..];
                    return pupilNames.TryGetValue(pupilId, out var pn)
                        ? string.Format(_l["ParentOfPupil"].Value, pn)
                        : _l["ParentWord"].Value;
                }

                return pupilNames.TryGetValue(userId, out var name) ? name : $"{_l["UserWord"]} {index + 1}";
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreateGroup()
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            return View("~/Views/Teacher/CreateGroup.cshtml", await BuildCreateGroupViewModel());
        }

        private async Task<CreateGroupViewModel> BuildCreateGroupViewModel(CreateGroupRequest? req = null, string? error = null) => new()
        {
            ClassGroups = await BuildClassGroups(),
            SubmitUrl = Url.Action(nameof(CreateGroup))!,
            CancelUrl = Url.Action(nameof(Groups))!,
            Error = error,
            Name = req?.Name ?? "",
            Type = req != null && Enum.IsDefined(typeof(GroupType), req.Type) ? req.Type : (int)GroupType.Pupils
        };

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
        public async Task<IActionResult> CreateGroup(CreateGroupRequest req)
        {
            var name = (req.Name ?? "").Trim();
            var typeValid = Enum.IsDefined(typeof(GroupType), req.Type);
            var image = req.Image is { Length: > 0 } ? req.Image : null;

            string? error = null;
            if (name.Length == 0 || name.Length > 200 || !typeValid)
                error = _l["GroupFormIncomplete"].Value;
            else if (image != null && !IsValidGroupImage(image))
                error = _l["GroupImageInvalid"].Value;

            if (error != null)
            {
                ViewBag.SidebarItems = _menuBuilder.ForTeacher();
                ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
                return View("~/Views/Teacher/CreateGroup.cshtml", await BuildCreateGroupViewModel(req, error));
            }

            var type = (GroupType)req.Type;
            var now = DateTime.Now;

            // անդամներ՝ սովորողների և ծնողների խմբում (միշտ ուսուցչի դասարանների սովորողներից)
            var memberIds = await ResolveGroupMemberIds(type, req.MemberIds);

            var group = new UserGroup
            {
                Name = name,
                Type = type,
                CreatedByUserId = CurrentTeacherUserId,
                CreatedAt = now,
                UpdatedAt = now,
                Members = new List<UserGroupMember>
                {
                    new() { UserId = CurrentTeacherUserId, Role = GroupMemberRole.Admin, AddedAt = now }
                }
            };
            foreach (var id in memberIds.Where(id => id != CurrentTeacherUserId))
                group.Members.Add(new UserGroupMember { UserId = id, Role = GroupMemberRole.Member, AddedAt = now });

            string? storedImagePath = null;
            try
            {
                if (image != null)
                {
                    var root = GroupImagesRoot();
                    Directory.CreateDirectory(root);

                    var stored = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
                    storedImagePath = Path.Combine(root, stored);
                    await using (var stream = System.IO.File.Create(storedImagePath))
                        await image.CopyToAsync(stream);

                    group.ImageStoredFileName = stored;
                    group.ImageContentType = image.ContentType;
                }

                _db.UserGroups.Add(group);
                await _db.SaveChangesAsync();
            }
            catch
            {
                if (storedImagePath != null)
                {
                    try { System.IO.File.Delete(storedImagePath); } catch { }
                }
                throw;
            }

            return RedirectToAction(nameof(Groups));
        }

        // Խմբի էջ՝ տեսնում են միայն խմբի անդամները
        [HttpGet]
        public async Task<IActionResult> GroupDetail(int id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var g = await _db.UserGroups
                .Where(x => x.Id == id && x.Members.Any(m => m.UserId == CurrentTeacherUserId))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Type,
                    x.ImageStoredFileName,
                    Members = x.Members
                        .OrderBy(m => m.Role)
                        .ThenBy(m => m.AddedAt)
                        .Select(m => new { m.UserId, m.Role })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (g == null) return NotFound();

            var nameFor = await GroupMemberNamer();

            var members = g.Members.Select((m, i) => new GroupMemberViewModel
            {
                Name = nameFor(m.UserId, i),
                IsAdmin = m.Role == GroupMemberRole.Admin,
                IsMe = m.UserId == CurrentTeacherUserId,
                RoleLabel = m.Role == GroupMemberRole.Admin ? _l["GroupRoleAdminShort"].Value : _l["GroupRoleMemberShort"].Value,
                ProfileUrl = "#"
            }).ToList();

            return View("~/Views/Teacher/GroupDetail.cshtml", new GroupDetailViewModel
            {
                Id = g.Id,
                Name = g.Name,
                ImageUrl = g.ImageStoredFileName == null ? null : Url.Action(nameof(GroupImage), new { id = g.Id }),
                TypeKey = g.Type.ToString(),
                TypeLabel = GroupTypeLabel(g.Type),
                MemberCount = members.Count,
                IsAdmin = g.Members.Any(m => m.UserId == CurrentTeacherUserId && m.Role == GroupMemberRole.Admin),
                Members = members,
                BackUrl = Url.Action(nameof(Groups))!,
                EditUrl = Url.Action(nameof(EditGroup), new { id = g.Id })!
            });
        }

        // ── Խմբի խմբագրում՝ միայն ադմինը ───────────────────────────────────────────

        private async Task<EditGroupViewModel?> BuildEditGroupViewModel(int id, EditGroupRequest? req = null, string? error = null)
        {
            var g = await _db.UserGroups
                .Where(x => x.Id == id
                         && x.Members.Any(m => m.UserId == CurrentTeacherUserId && m.Role == GroupMemberRole.Admin))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Type,
                    x.ImageStoredFileName,
                    Members = x.Members.OrderBy(m => m.Role).ThenBy(m => m.AddedAt).Select(m => new { m.UserId, m.Role }).ToList()
                })
                .FirstOrDefaultAsync();

            if (g == null) return null;

            var nameFor = await GroupMemberNamer();
            var memberIds = g.Members.Select(m => m.UserId).ToHashSet();

            // նոր անդամներ ավելացնելու ցուցակ՝ առանց արդեն խմբում եղածների
            var classGroups = new List<AssignmentClassGroupViewModel>();
            if (g.Type == GroupType.Pupils || g.Type == GroupType.Parents)
            {
                foreach (var cg in await BuildClassGroups())
                {
                    cg.Pupils = cg.Pupils
                        .Where(p => !memberIds.Contains(g.Type == GroupType.Parents ? ParentIdPrefix + p.UserId : p.UserId))
                        .ToList();
                    classGroups.Add(cg);
                }
            }

            return new EditGroupViewModel
            {
                Id = g.Id,
                Name = req?.Name ?? g.Name,
                ImageUrl = g.ImageStoredFileName == null ? null : Url.Action(nameof(GroupImage), new { id = g.Id }),
                TypeValue = (int)g.Type,
                TypeKey = g.Type.ToString(),
                TypeLabel = GroupTypeLabel(g.Type),
                Members = g.Members.Select((m, i) => new EditGroupMemberViewModel
                {
                    UserId = m.UserId,
                    Name = nameFor(m.UserId, i),
                    IsAdmin = m.Role == GroupMemberRole.Admin,
                    RoleLabel = m.Role == GroupMemberRole.Admin ? _l["GroupRoleAdminShort"].Value : _l["GroupRoleMemberShort"].Value
                }).ToList(),
                ClassGroups = classGroups,
                SubmitUrl = Url.Action(nameof(EditGroup), new { id = g.Id })!,
                CancelUrl = Url.Action(nameof(GroupDetail), new { id = g.Id })!,
                Error = error
            };
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(int id)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var vm = await BuildEditGroupViewModel(id);
            if (vm == null) return NotFound();

            return View("~/Views/Teacher/EditGroup.cshtml", vm);
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
        public async Task<IActionResult> EditGroup(int id, EditGroupRequest req)
        {
            var g = await _db.UserGroups
                .Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.Id == id
                                       && x.Members.Any(m => m.UserId == CurrentTeacherUserId && m.Role == GroupMemberRole.Admin));
            if (g == null) return NotFound();

            var name = (req.Name ?? "").Trim();
            var image = req.Image is { Length: > 0 } ? req.Image : null;

            string? error = null;
            if (name.Length == 0 || name.Length > 200)
                error = _l["GroupFormIncomplete"].Value;
            else if (image != null && !IsValidGroupImage(image))
                error = _l["GroupImageInvalid"].Value;

            if (error != null)
            {
                ViewBag.SidebarItems = _menuBuilder.ForTeacher();
                ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
                var vm = await BuildEditGroupViewModel(id, req, error);
                return View("~/Views/Teacher/EditGroup.cshtml", vm);
            }

            var now = DateTime.Now;

            // անդամների հեռացում (ադմինին՝ ստեղծողին, հեռացնել չի կարելի)
            var removeIds = req.RemoveMemberIds.ToHashSet();
            _db.UserGroupMembers.RemoveRange(g.Members.Where(m => m.Role != GroupMemberRole.Admin && removeIds.Contains(m.UserId)));

            // նոր անդամներ (խմբի տեսակը չի փոխվում)
            var existing = g.Members.Select(m => m.UserId).ToHashSet();
            foreach (var uid in (await ResolveGroupMemberIds(g.Type, req.AddMemberIds)).Where(u => !existing.Contains(u) && u != CurrentTeacherUserId))
                g.Members.Add(new UserGroupMember { UserId = uid, Role = GroupMemberRole.Member, AddedAt = now });

            g.Name = name;
            g.UpdatedAt = now;

            var oldImage = g.ImageStoredFileName;
            string? newImagePath = null;
            try
            {
                if (image != null)
                {
                    var root = GroupImagesRoot();
                    Directory.CreateDirectory(root);

                    var stored = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
                    newImagePath = Path.Combine(root, stored);
                    await using (var stream = System.IO.File.Create(newImagePath))
                        await image.CopyToAsync(stream);

                    g.ImageStoredFileName = stored;
                    g.ImageContentType = image.ContentType;
                }
                else if (req.RemoveImage)
                {
                    g.ImageStoredFileName = null;
                    g.ImageContentType = null;
                }

                await _db.SaveChangesAsync();
            }
            catch
            {
                if (newImagePath != null)
                {
                    try { System.IO.File.Delete(newImagePath); } catch { }
                }
                throw;
            }

            // հին նկարը ջնջվում է միայն հաջող պահպանումից հետո
            if (!string.IsNullOrEmpty(oldImage) && oldImage != g.ImageStoredFileName)
            {
                try { System.IO.File.Delete(Path.Combine(GroupImagesRoot(), Path.GetFileName(oldImage))); } catch { }
            }

            return RedirectToAction(nameof(GroupDetail), new { id });
        }

        public record DeleteGroupRequest(int Id);

        // Խմբի ջնջում՝ միայն ադմինը
        [HttpPost]
        public async Task<IActionResult> DeleteGroup([FromBody] DeleteGroupRequest req)
        {
            var g = await _db.UserGroups
                .Where(x => x.Id == req.Id
                         && x.Members.Any(m => m.UserId == CurrentTeacherUserId && m.Role == GroupMemberRole.Admin))
                .Select(x => new { x.Id, x.ImageStoredFileName })
                .FirstOrDefaultAsync();

            if (g == null) return NotFound();

            // անդամները ջնջվում են միասին (cascade)
            await _db.UserGroups.Where(x => x.Id == g.Id).ExecuteDeleteAsync();

            if (!string.IsNullOrEmpty(g.ImageStoredFileName))
            {
                try { System.IO.File.Delete(Path.Combine(GroupImagesRoot(), Path.GetFileName(g.ImageStoredFileName))); } catch { }
            }

            return Ok();
        }

        // Խմբի նկարը՝ տեսնում են միայն խմբի անդամները
        [HttpGet]
        public async Task<IActionResult> GroupImage(int id)
        {
            var g = await _db.UserGroups
                .Where(x => x.Id == id && x.Members.Any(m => m.UserId == CurrentTeacherUserId))
                .Select(x => new { x.ImageStoredFileName, x.ImageContentType })
                .FirstOrDefaultAsync();

            if (g == null || string.IsNullOrEmpty(g.ImageStoredFileName)) return NotFound();

            var path = Path.Combine(GroupImagesRoot(), Path.GetFileName(g.ImageStoredFileName));
            if (!System.IO.File.Exists(path)) return NotFound();

            return File(System.IO.File.OpenRead(path),
                string.IsNullOrWhiteSpace(g.ImageContentType) ? "application/octet-stream" : g.ImageContentType);
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

        // Ուսուցչի դասարանները (վերջին ուսումնական տարով) և նրանց աշակերտները
        private async Task<List<AssignmentClassGroupViewModel>> BuildClassGroups()
        {
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

            return classGroups;
        }

        // Սխալի դեպքում ձևը վերադարձվում է արդեն լրացված տվյալներով (ոչինչ չի կորչում)
        private async Task<CreateAssignmentViewModel> BuildCreateAssignmentViewModelFromRequest(CreateAssignmentRequest req, string error)
        {
            var vm = await BuildCreateAssignmentViewModel(error);

            var prefill = new CreateAssignmentPrefill
            {
                SourceTemplateId = req.SourceTemplateId,
                Title = req.Title ?? "",
                Description = req.Description,
                SubjectIds = req.SubjectIds.ToList(),
                GradeLevels = req.GradeLevels.ToList(),
                Types = req.Types.Where(t => t != 0 && Enum.IsDefined(typeof(AssignmentTypes), t)).ToList(),
                OtherTypeText = req.OtherTypeText,
                GradingType = req.GradingType,
                AllowTextAnswer = req.AllowTextAnswer,
                AllowFileAnswer = req.AllowFileAnswer,
                StartDate = req.StartDate == default ? null : req.StartDate.ToString("yyyy-MM-ddTHH:mm"),
                DueDate = req.DueDate == default ? null : req.DueDate.ToString("yyyy-MM-ddTHH:mm"),
                PupilIds = req.PupilIds.ToList()
            };

            if (req.SourceTemplateId.HasValue)
            {
                prefill.ExistingFiles = await _db.AssignmentFiles
                    .Where(f => f.AssignmentId == req.SourceTemplateId.Value
                             && f.Assignment.TeacherUserId == CurrentTeacherUserId
                             && f.Assignment.IsTemplate)
                    .OrderBy(f => f.Id)
                    .Select(f => new PrefillFile { Id = f.Id, Name = f.FileName, Keep = req.KeepFileIds.Contains(f.Id) })
                    .ToListAsync();

                vm.CancelUrl = Url.Action(nameof(Repository), new { id = "AssignmentBank" })!;
            }

            vm.Prefill = prefill;
            vm.Mode = req.SourceTemplateId.HasValue ? "Send" : "Create";
            return vm;
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

            var classGroups = await BuildClassGroups();

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
        public async Task<IActionResult> CreateAssignment(int? fromTemplate)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();

            var vm = await BuildCreateAssignmentViewModel();

            // Շտեմարանից բացում՝ նույն ձևը՝ նախալրացված տվյալներով
            if (fromTemplate.HasValue)
            {
                vm.Prefill = await LoadAssignmentPrefill(fromTemplate.Value);
                if (vm.Prefill == null) return NotFound();
                vm.Mode = "Send";
                vm.CancelUrl = Url.Action(nameof(Repository), new { id = "AssignmentBank" })!;
            }

            return View("~/Views/Teacher/Repository/CreateAssignment.cshtml", vm);
        }

        private async Task<CreateAssignmentPrefill?> LoadAssignmentPrefill(int templateId)
        {
            var t = await _db.Assignments
                .Where(x => x.Id == templateId && x.TeacherUserId == CurrentTeacherUserId && x.IsTemplate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.Types,
                    x.OtherTypeText,
                    x.GradingType,
                    x.ResponseForms,
                    SubjectIds = x.Subjects.Select(s => s.SubjectId).ToList(),
                    Levels = x.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
                    Files = x.Files.OrderBy(f => f.Id).Select(f => new PrefillFile { Id = f.Id, Name = f.FileName }).ToList()
                })
                .FirstOrDefaultAsync();

            if (t == null) return null;

            return new CreateAssignmentPrefill
            {
                SourceTemplateId = t.Id,
                Title = t.Title,
                Description = t.Description,
                SubjectIds = t.SubjectIds,
                GradeLevels = t.Levels,
                Types = Enum.GetValues<AssignmentTypes>()
                    .Where(f => f != AssignmentTypes.None && t.Types.HasFlag(f))
                    .Select(f => (int)f)
                    .ToList(),
                OtherTypeText = t.OtherTypeText,
                GradingType = (int)t.GradingType,
                AllowTextAnswer = t.ResponseForms.HasFlag(AssignmentResponseForms.Text),
                AllowFileAnswer = t.ResponseForms.HasFlag(AssignmentResponseForms.File),
                ExistingFiles = t.Files
            };
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
                || req.DueDate.Date < req.StartDate.Date;

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
                var errorText = req.DueDate.Date < req.StartDate.Date && req.StartDate != default
                    ? _l["DeadlineInvalid"].Value
                    : _l["AssignmentFormIncomplete"].Value;
                return View("~/Views/Teacher/Repository/CreateAssignment.cshtml",
                    await BuildCreateAssignmentViewModelFromRequest(req, errorText));
            }

            // ── կցված ֆայլերը (սերվերի ֆայլային պահոցում) ─────────────────────
            var attachments = req.Attachments.Where(f => f.Length > 0).ToList();
            if (attachments.Count + req.KeepFileIds.Count > MaxAttachments
                || attachments.Any(f => f.Length > MaxAttachmentBytes
                                        || !AllowedAttachmentExtensions.Contains(Path.GetExtension(f.FileName))))
            {
                ViewBag.SidebarItems = _menuBuilder.ForTeacher();
                ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
                return View("~/Views/Teacher/Repository/CreateAssignment.cshtml",
                                    await BuildCreateAssignmentViewModelFromRequest(req, _l["AssignmentFormIncomplete"].Value));
            }

            // շտեմարանի ձևանմուշից թողնված ֆայլերը (նույն ֆայլը՝ նոր գրառումով)
            var keptFiles = new List<AssignmentFile>();
            if (req.SourceTemplateId.HasValue && req.KeepFileIds.Count > 0)
            {
                keptFiles = await _db.AssignmentFiles
                    .AsNoTracking()
                    .Where(f => req.KeepFileIds.Contains(f.Id)
                             && f.AssignmentId == req.SourceTemplateId.Value
                             && f.Assignment.TeacherUserId == CurrentTeacherUserId
                             && f.Assignment.IsTemplate)
                    .ToListAsync();
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

                // ── պահպանում՝ մեկ ուղարկված առաջադրանք (շտեմարանը նույն գրառումներն են) ──
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
                    }).Concat(keptFiles.Select(k => new AssignmentFile
                    {
                        FileName = k.FileName,
                        StoredFileName = k.StoredFileName,
                        ContentType = k.ContentType,
                        SizeBytes = k.SizeBytes,
                        UploadedAt = now
                    })).ToList()
                };

                var sent = Make(false);
                sent.Recipients = pupilGrades.Select(p => new AssignmentRecipient
                {
                    PupilUserId = p.PupilUserId,
                    GradeId = p.GradeId,
                    Status = AssignmentRecipientStatus.Sent,
                    SentAt = now
                }).ToList();

                // Շտեմարանից ուղարկելիս՝ միայն նոր ուղարկված օրինակ (շտեմարանի գրառումը նույնն է մնում)
                var source = req.SourceTemplateId.HasValue
                    ? await _db.Assignments.FirstOrDefaultAsync(x => x.Id == req.SourceTemplateId.Value
                                                                  && x.TeacherUserId == CurrentTeacherUserId
                                                                  && x.IsTemplate)
                    : null;

                if (source != null)
                {
                    sent.TemplateId = source.Id;
                    _db.Assignments.Add(sent);
                }
                else
                {
                    var template = Make(true);
                    sent.Template = template;
                    _db.Assignments.AddRange(template, sent);
                }

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

            var classGroups = await BuildClassGroups();
            var nameByUser = classGroups
                .SelectMany(g => g.Pupils)
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.First().Name);

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
                StartDateRaw = a.StartDate,
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
                    PupilName = nameByUser.TryGetValue(r.PupilUserId, out var pupilName) ? pupilName : $"{_l["Pupil"]} {i + 1}",
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

            vm.AvailableGrades = classGroups;
            vm.AlreadyAddedPupilUserIds = a.Recipients.Select(r => r.PupilUserId).ToList();

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
            public List<string> PupilIds { get; set; } = new();
        }

        // Առաջադրանքին նոր աշակերտներ ավելացնել (միայն ուսուցչի դասարաններից)
        [HttpPost]
        public async Task<IActionResult> AddPupilsToAssignment([FromBody] AddPupilsRequest req)
        {
            var assignment = await _db.Assignments
                .Where(x => x.Id == req.AssignmentId
                         && x.TeacherUserId == CurrentTeacherUserId
                         && !x.IsTemplate
                         && !x.IsArchived)
                .Select(x => new { x.Id })
                .FirstOrDefaultAsync();
            if (assignment == null) return NotFound();

            var wanted = req.PupilIds.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (wanted.Count == 0) return BadRequest();

            var existing = await _db.AssignmentRecipients
                .Where(r => r.AssignmentId == assignment.Id && wanted.Contains(r.PupilUserId))
                .Select(r => r.PupilUserId)
                .ToListAsync();

            var teacherGradeIds = await _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentTeacherUserId)
                .Select(tg => tg.GradeId)
                .ToListAsync();

            var toAdd = (await _db.PupilGrades
                    .Where(pg => wanted.Contains(pg.PupilUserId) && teacherGradeIds.Contains(pg.GradeId))
                    .Select(pg => new { pg.PupilUserId, pg.GradeId })
                    .ToListAsync())
                .GroupBy(p => p.PupilUserId)
                .Where(g => !existing.Contains(g.Key))
                .Select(g => new AssignmentRecipient
                {
                    AssignmentId = assignment.Id,
                    PupilUserId = g.Key,
                    GradeId = g.First().GradeId,
                    Status = AssignmentRecipientStatus.Sent,
                    SentAt = DateTime.Now
                })
                .ToList();

            if (toAdd.Count == 0) return BadRequest();

            _db.AssignmentRecipients.AddRange(toAdd);
            await _db.SaveChangesAsync();

            return Ok(new { added = toAdd.Count });
        }

        public class ChangeDueDateRequest
        {
            public int Id { get; set; }
            public DateTime DueDate { get; set; }
        }

        // Վերջնաժամկետի փոփոխություն
        [HttpPost]
        public async Task<IActionResult> ChangeDueDate([FromBody] ChangeDueDateRequest req)
        {
            var a = await _db.Assignments
                .Where(x => x.Id == req.Id && x.TeacherUserId == CurrentTeacherUserId && !x.IsTemplate)
                .Select(x => new { x.StartDate })
                .FirstOrDefaultAsync();
            if (a == null) return NotFound();

            if (a.StartDate.HasValue && req.DueDate.Date < a.StartDate.Value.Date) return BadRequest();

            await _db.Assignments
                .Where(x => x.Id == req.Id && x.TeacherUserId == CurrentTeacherUserId && !x.IsTemplate)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.DueDate, req.DueDate)
                    .SetProperty(x => x.UpdatedAt, DateTime.Now));

            return Ok();
        }
    }
}
