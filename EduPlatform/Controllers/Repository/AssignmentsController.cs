using EduPlatform.Data;
using EduPlatform.Data.Models;
using EduPlatform.Resources;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using EduPlatform.ViewModels.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// «Իմ առաջադրանքները»՝ «Իմ գրապահոց» բաժնի մաս։ Մեկ կոնտրոլեր։
    /// Հասցեները՝ /Repository/Assignments/... (տրված, շտեմարան, ստեղծում)
    /// Էջերը՝ Views/Shared/Repository/Assignments/{Given|Bank|Form}/
    /// Առայժմ միայն ուսուցչի էջերն են. սովորողի էջերը կավելանան այստեղ՝ Pupil դերով action-ներով։
    /// </summary>
    [Authorize(Roles = "Teacher")]
    [Route("Repository/Assignments")]
    public class AssignmentsController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly EduPlatformDbContext _db;
        private readonly IConfiguration _config;
        private readonly IClassRosterService _roster;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public AssignmentsController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer,
            EduPlatformDbContext db, IConfiguration config, IClassRosterService roster)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
            _config = config;
            _roster = roster;
        }

        // Կողային մենյուն ամեն էջի համար
        // TODO (սովորողի քայլ)՝ սովորողի համար՝ ForPupil()
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
            base.OnActionExecuting(context);
        }

        // ── Ցուցակները (մուտքի էջերը) ──────────────────────────────────────────

        // «Տրված առաջադրանքներ»՝ ընթացիկ և արխիվացված
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            return View("Given/Index", await BuildAssignmentsPage());
        }

        // «Առաջադրանքների շտեմարան»
        [HttpGet("Bank")]
        public async Task<IActionResult> Bank()
        {
            return View("Bank/Index", await BuildAssignmentBank());
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
                .Where(a => a.TeacherUserId == CurrentUserId && !a.IsTemplate)
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
                    ViewUrl = Url.Action(nameof(Detail), new { id = r.Id })!
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
                CreateUrl = Url.Action(nameof(Create))!
            };
        }

        // ── «Առաջադրանքների շտեմարան» ────────────────────────────────────────

        private async Task<AssignmentBankViewModel> BuildAssignmentBank()
        {
            var rows = await _db.Assignments
                .Where(a => a.TeacherUserId == CurrentUserId && a.IsTemplate)
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
                CreateUrl = Url.Action(nameof(Create))!
            };
        }

        // ── Շտեմարանի գրառման դիտում / խմբագրում ──────────────────────────────

        [HttpGet("Bank/{id:int}")]
        public async Task<IActionResult> BankView(int id)
        {

            var t = await _db.Assignments
                .Where(x => x.Id == id && x.TeacherUserId == CurrentUserId && x.IsTemplate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.TaskText,
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
                TaskText = t.TaskText ?? "",
                AllowTextAnswer = t.ResponseForms.HasFlag(AssignmentResponseForms.Text),
                AllowFileAnswer = t.ResponseForms.HasFlag(AssignmentResponseForms.File),
                Files = t.Files.Select(f => new AssignmentFileViewModel
                {
                    Name = f.FileName,
                    ViewUrl = Url.Action(nameof(AssignmentFile), new { id = f.Id })!,
                    DownloadUrl = Url.Action(nameof(AssignmentFile), new { id = f.Id, download = true })!
                }).ToList()
            };

            ViewBag.SendUrl = Url.Action(nameof(Create), new { fromTemplate = id });
            ViewBag.EditUrl = Url.Action(nameof(BankEdit), new { id });
            ViewBag.BackUrl = Url.Action(nameof(Bank));

            return View("Bank/View", vm);
        }

        [HttpGet("Bank/{id:int}/Edit")]
        public async Task<IActionResult> BankEdit(int id)
        {

            var vm = await BuildCreateAssignmentViewModel();
            vm.Prefill = await LoadAssignmentPrefill(id);
            if (vm.Prefill == null) return NotFound();

            vm.Mode = "Edit";
            vm.SubmitUrl = Url.Action(nameof(BankEdit), new { id })!;
            vm.CancelUrl = Url.Action(nameof(Bank))!;

            return View("Form/Create", vm);
        }

        [HttpPost("Bank/{id:int}/Edit")]
        [RequestSizeLimit(150_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 150_000_000)]
        public async Task<IActionResult> BankEdit(int id, CreateAssignmentRequest req)
        {
            var a = await _db.Assignments
                .Include(x => x.Subjects)
                .Include(x => x.GradeLevels)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id && x.TeacherUserId == CurrentUserId && x.IsTemplate);
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
                : await ValidTeacherSubjectIds(subjectIds);

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

                req.SourceTemplateId = id;
                var vm = await BuildCreateAssignmentViewModelFromRequest(req, _l["AssignmentFormIncomplete"].Value);
                vm.Mode = "Edit";
                vm.SubmitUrl = Url.Action(nameof(BankEdit), new { id })!;
                vm.CancelUrl = Url.Action(nameof(Bank))!;
                return View("Form/Create", vm);
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
                a.TaskText = string.IsNullOrWhiteSpace(req.TaskText) ? null : req.TaskText.Trim();
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

            return RedirectToAction(nameof(Bank));
        }

        public record DeleteBankAssignmentRequest(int Id);

        // Շտեմարանից ջնջում՝ միայն շտեմարանի գրառումը, արդեն ուղարկվածները (աշակերտների աշխատանքով) մնում են
        [HttpPost("Bank/Delete")]
        public async Task<IActionResult> BankDelete([FromBody] DeleteBankAssignmentRequest req)
        {
            var exists = await _db.Assignments
                .AnyAsync(a => a.Id == req.Id && a.TeacherUserId == CurrentUserId && a.IsTemplate);
            if (!exists) return NotFound();

            await using var tx = await _db.Database.BeginTransactionAsync();

            await _db.Assignments
                .Where(a => a.TemplateId == req.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TemplateId, (int?)null));

            await _db.Assignments
                .Where(a => a.Id == req.Id && a.TeacherUserId == CurrentUserId && a.IsTemplate)
                .ExecuteDeleteAsync();

            await tx.CommitAsync();
            return Ok();
        }

        public record ArchiveAssignmentRequest(int Id);

        // «Արխիվացնել»՝ ընթացիկ առաջադրանքը տեղափոխվում է «Արխիվացված»
        [HttpPost("Archive")]
        public async Task<IActionResult> Archive([FromBody] ArchiveAssignmentRequest req)
        {
            var affected = await _db.Assignments
                .Where(a => a.Id == req.Id
                         && a.TeacherUserId == CurrentUserId
                         && !a.IsTemplate
                         && !a.IsArchived)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.IsArchived, true)
                    .SetProperty(a => a.ArchivedAt, DateTime.Now));

            return affected > 0 ? Ok() : NotFound();
        }


        // ── Առաջադրանքի ստեղծում ─────────────────────────────────────────────

        private const int MaxAttachments = 10;
        private const long MaxAttachmentBytes = 25L * 1024 * 1024;

        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt",
            ".png", ".jpg", ".jpeg", ".zip"
        };

        // Միայն այն առարկաները, որոնք ուսուցիչը դասավանդում է
        private Task<List<int>> ValidTeacherSubjectIds(IEnumerable<int> requested)
        {
            var wanted = requested.Distinct().ToList();
            return _db.TeacherSubjects
                .Where(ts => ts.TeacherUserId == CurrentUserId && wanted.Contains(ts.SubjectId))
                .Select(ts => ts.SubjectId)
                .ToListAsync();
        }

        private string AssignmentFilesRoot()
        {
            var configured = _config["AssignmentFiles:RootPath"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured;

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return Path.Combine(env.ContentRootPath, "App_Data", "assignment-files");
        }

        // Ուսուցչի դասարանները (վերջին ուսումնական տարով) և նրանց աշակերտները

        // Սխալի դեպքում ձևը վերադարձվում է արդեն լրացված տվյալներով (ոչինչ չի կորչում)
        private async Task<CreateAssignmentViewModel> BuildCreateAssignmentViewModelFromRequest(CreateAssignmentRequest req, string error)
        {
            var vm = await BuildCreateAssignmentViewModel(error);

            var prefill = new CreateAssignmentPrefill
            {
                SourceTemplateId = req.SourceTemplateId,
                Title = req.Title ?? "",
                Description = req.Description,
                TaskText = req.TaskText,
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
                             && f.Assignment.TeacherUserId == CurrentUserId
                             && f.Assignment.IsTemplate)
                    .OrderBy(f => f.Id)
                    .Select(f => new PrefillFile { Id = f.Id, Name = f.FileName, Keep = req.KeepFileIds.Contains(f.Id) })
                    .ToListAsync();

                vm.CancelUrl = Url.Action(nameof(Bank))!;
            }

            vm.Prefill = prefill;
            vm.Mode = req.SourceTemplateId.HasValue ? "Send" : "Create";
            return vm;
        }

        // Ուսուցչի «Սովորողների» խմբերը («Իմ խմբերը» ներդիրի համար)։
        // alreadyAddedPupilIds՝ արդեն առաջադրանք ստացած սովորողները ցուցադրվում են, բայց չեն ընտրվում (Ավելացնել սովորողներ)
        private async Task<List<AssignmentGroupOptionViewModel>> BuildGroupOptions(
            List<AssignmentClassGroupViewModel> classGroups, ICollection<string>? alreadyAddedPupilIds = null)
        {
            var me = CurrentUserId;
            var groupRows = await _db.UserGroupMembers
                .Where(m => m.UserId == me)
                .OrderBy(m => m.Group.Name)
                .Select(m => new
                {
                    m.Group.Id,
                    m.Group.Name,
                    m.Group.Type,
                    m.Group.ImageStoredFileName,
                    MemberIds = m.Group.Members.Where(x => x.UserId != me).Select(x => x.UserId).ToList()
                })
                .ToListAsync();

            // միայն ուսուցչի դասարանների սովորողները կարելի է ընտրել (նրանց անուններն էլ այստեղից են)
            var pupilById = classGroups
                .SelectMany(cg => cg.Pupils)
                .GroupBy(p => p.UserId)
                .ToDictionary(p => p.Key, p => p.First());

            return groupRows.Select(g =>
            {
                // Սովորողների խումբ՝ անդամները սովորողներն են։ Ծնողների խումբ՝ առաջադրանքը ստանում են ծնողների երեխաները
                // (ժամանակավոր ծնողի Id-ն՝ «parent:{սովորողի UserId}»)։ «Այլ» խումբը սովորողներ չունի։
                var pupilIds = g.Type switch
                {
                    GroupType.Pupils => g.MemberIds,
                    GroupType.Parents => g.MemberIds
                        .Where(id => id.StartsWith("parent:"))
                        .Select(id => id["parent:".Length..])
                        .ToList(),
                    _ => new List<string>()
                };

                return new AssignmentGroupOptionViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    ImageUrl = g.ImageStoredFileName == null ? null : Url.Action("Image", "Groups", new { id = g.Id }),
                    Pupils = pupilIds
                        .Where(pupilById.ContainsKey)
                        .Select(id => pupilById[id])
                        .ToList(),
                    DisabledPupilIds = pupilIds
                        .Where(id => alreadyAddedPupilIds != null && alreadyAddedPupilIds.Contains(id))
                        .ToList()
                };
            }).ToList();
        }

        private async Task<CreateAssignmentViewModel> BuildCreateAssignmentViewModel(string? error = null)
        {
            var subjects = await _db.TeacherSubjects
                .Where(ts => ts.TeacherUserId == CurrentUserId)
                .Select(ts => new SelectOption { Id = ts.SubjectId, Label = ts.Subject.Name })
                .OrderBy(s => s.Label)
                .ToListAsync();

            var classGroups = await _roster.BuildClassGroupsAsync(CurrentUserId);

            var groups = await BuildGroupOptions(classGroups);

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
                Groups = groups,
                SubmitUrl = Url.Action(nameof(Create))!,
                CancelUrl = Url.Action(nameof(Index))!,
                Error = error
            };
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create(int? fromTemplate)
        {

            var vm = await BuildCreateAssignmentViewModel();

            // Շտեմարանից բացում՝ նույն ձևը՝ նախալրացված տվյալներով
            if (fromTemplate.HasValue)
            {
                vm.Prefill = await LoadAssignmentPrefill(fromTemplate.Value);
                if (vm.Prefill == null) return NotFound();
                vm.Mode = "Send";
                vm.CancelUrl = Url.Action(nameof(Bank))!;
            }

            return View("Form/Create", vm);
        }

        private async Task<CreateAssignmentPrefill?> LoadAssignmentPrefill(int templateId)
        {
            var t = await _db.Assignments
                .Where(x => x.Id == templateId && x.TeacherUserId == CurrentUserId && x.IsTemplate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.TaskText,
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
                TaskText = t.TaskText,
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

        [HttpPost("Create")]
        [RequestSizeLimit(150_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 150_000_000)]
        public async Task<IActionResult> Create(CreateAssignmentRequest req)
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
                .Where(tg => tg.TeacherUserId == CurrentUserId)
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

                validSubjectIds = await ValidTeacherSubjectIds(subjectIds);
            }

            if (incomplete || pupilGrades.Count == 0 || validSubjectIds.Count == 0)
            {
                var errorText = req.DueDate.Date < req.StartDate.Date && req.StartDate != default
                    ? _l["DeadlineInvalid"].Value
                    : _l["AssignmentFormIncomplete"].Value;
                return View("Form/Create",
                    await BuildCreateAssignmentViewModelFromRequest(req, errorText));
            }

            // ── կցված ֆայլերը (սերվերի ֆայլային պահոցում) ─────────────────────
            var attachments = req.Attachments.Where(f => f.Length > 0).ToList();
            if (attachments.Count + req.KeepFileIds.Count > MaxAttachments
                || attachments.Any(f => f.Length > MaxAttachmentBytes
                                        || !AllowedAttachmentExtensions.Contains(Path.GetExtension(f.FileName))))
            {
                return View("Form/Create",
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
                             && f.Assignment.TeacherUserId == CurrentUserId
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
                    TeacherUserId = CurrentUserId,
                    IsTemplate = isTemplate,
                    Title = req.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                    TaskText = string.IsNullOrWhiteSpace(req.TaskText) ? null : req.TaskText.Trim(),
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
                                                                  && x.TeacherUserId == CurrentUserId
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

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {

            var a = await _db.Assignments
                .Where(x => x.Id == id && x.TeacherUserId == CurrentUserId && !x.IsTemplate)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.TaskText,
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

            var classGroups = await _roster.BuildClassGroupsAsync(CurrentUserId);
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
                TaskText = a.TaskText ?? "",
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
            vm.Groups = await BuildGroupOptions(classGroups, vm.AlreadyAddedPupilUserIds);

            return View("Given/Detail", vm);
        }

        // Ուսուցչի կցած ֆայլի բացում (նոր էջում) կամ ներբեռնում
        [HttpGet("Files/{id:int}")]
        public async Task<IActionResult> AssignmentFile(int id, bool download = false)
        {
            var f = await _db.AssignmentFiles
                .Where(x => x.Id == id && x.Assignment.TeacherUserId == CurrentUserId)
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
        [HttpGet("SubmissionFiles/{id:int}")]
        public async Task<IActionResult> SubmissionFile(int id, bool download = false)
        {
            var f = await _db.AssignmentSubmissionFiles
                .Where(x => x.Id == id && x.Recipient.Assignment.TeacherUserId == CurrentUserId)
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
        [HttpPost("Grade")]
        public async Task<IActionResult> SaveGrade([FromBody] SaveGradeRequest req)
        {
            var r = await _db.AssignmentRecipients
                .Include(x => x.Assignment)
                .FirstOrDefaultAsync(x => x.Id == req.SubmissionId
                                       && x.Assignment.TeacherUserId == CurrentUserId
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
        [HttpPost("AddPupils")]
        public async Task<IActionResult> AddPupils([FromBody] AddPupilsRequest req)
        {
            var assignment = await _db.Assignments
                .Where(x => x.Id == req.AssignmentId
                         && x.TeacherUserId == CurrentUserId
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
                .Where(tg => tg.TeacherUserId == CurrentUserId)
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
        [HttpPost("ChangeDueDate")]
        public async Task<IActionResult> ChangeDueDate([FromBody] ChangeDueDateRequest req)
        {
            var a = await _db.Assignments
                .Where(x => x.Id == req.Id && x.TeacherUserId == CurrentUserId && !x.IsTemplate)
                .Select(x => new { x.StartDate })
                .FirstOrDefaultAsync();
            if (a == null) return NotFound();

            if (a.StartDate.HasValue && req.DueDate.Date < a.StartDate.Value.Date) return BadRequest();

            await _db.Assignments
                .Where(x => x.Id == req.Id && x.TeacherUserId == CurrentUserId && !x.IsTemplate)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.DueDate, req.DueDate)
                    .SetProperty(x => x.UpdatedAt, DateTime.Now));

            return Ok();
        }
    }
}
