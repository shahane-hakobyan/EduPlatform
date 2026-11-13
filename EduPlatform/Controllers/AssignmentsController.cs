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
        private readonly IFileStorageService _files;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public AssignmentsController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer,
            EduPlatformDbContext db, IConfiguration config, IClassRosterService roster, IFileStorageService files)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
            _config = config;
            _roster = roster;
            _files = files;
        }

        // Կողային մենյուն ամեն էջի համար  (ըստ դերի՝ ուսուցչի կամ սովորողի)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForUser(User);
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForUser(User);
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
            // տրված առաջադրանքներ՝ բովանդակությունը վերցվում է կապված շտեմարանի առաջադրանքից
            var rows = await _db.GivenAssignments
                .Where(a => a.TeacherUserId == CurrentUserId)
                .OrderBy(a => a.DueDate)
                .Select(a => new
                {
                    a.Id,
                    a.Assignment.Title,
                    a.Assignment.Types,
                    a.Assignment.OtherTypeText,
                    a.Assignment.GradingType,
                    a.StartDate,
                    a.DueDate,
                    a.IsArchived,
                    Subjects = a.Assignment.Subjects.Select(s => s.Subject.Name).ToList(),
                    Levels = a.Assignment.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
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
                    DueDate = r.DueDate,
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
                .Where(a => a.TeacherUserId == CurrentUserId && a.IsInBank)
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
                    SentCount = a.GivenAssignments.Count()
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

        // Շտեմարանի գրառման ամփոփումը (օգտագործվում է «Դիտել» և «Ուղարկել» էջերում)
        private async Task<AssignmentDetailViewModel?> LoadBankDetail(int id)
        {
            var t = await _db.Assignments
                .Where(x => x.Id == id && x.TeacherUserId == CurrentUserId && x.IsInBank)
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
            if (t == null) return null;

            return new AssignmentDetailViewModel
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
        }

        [HttpGet("Bank/{id:int}")]
        public async Task<IActionResult> BankView(int id)
        {
            var vm = await LoadBankDetail(id);
            if (vm == null) return NotFound();

            ViewBag.SendUrl = Url.Action(nameof(BankSend), new { id });
            ViewBag.EditUrl = Url.Action(nameof(BankEdit), new { id });
            ViewBag.BackUrl = Url.Action(nameof(Bank));

            return View("Bank/View", vm);
        }

        // ── Շտեմարանից ուղարկում՝ միայն սովորողների ընտրություն + ժամկետներ ──────

        private async Task<SendAssignmentViewModel?> BuildSendViewModel(int id, string? error = null)
        {
            var detail = await LoadBankDetail(id);
            if (detail == null) return null;

            var classGroups = await _roster.BuildClassGroupsAsync(CurrentUserId);
            var now = DateTime.Now;

            return new SendAssignmentViewModel
            {
                Assignment = detail,
                ClassGroups = classGroups,
                Groups = await BuildGroupOptions(classGroups),
                SubmitUrl = Url.Action(nameof(BankSend), new { id })!,
                CancelUrl = Url.Action(nameof(Bank))!,
                StartDate = now.ToString("yyyy-MM-ddTHH:mm"),
                DueDate = "",
                Error = error
            };
        }

        [HttpGet("Bank/{id:int}/Send")]
        public async Task<IActionResult> BankSend(int id)
        {
            var vm = await BuildSendViewModel(id);
            if (vm == null) return NotFound();
            return View("Form/Send", vm);
        }

        [HttpPost("Bank/{id:int}/Send")]
        public async Task<IActionResult> BankSend(int id, SendAssignmentRequest req)
        {
            var exists = await _db.Assignments
                .AnyAsync(x => x.Id == id && x.TeacherUserId == CurrentUserId && x.IsInBank);
            if (!exists) return NotFound();

            var pupilIds = req.PupilIds.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

            var datesOk = req.StartDate != default && req.DueDate != default && req.DueDate.Date >= req.StartDate.Date;

            // ընտրված աշակերտները պետք է լինեն ՄԻԱՅՆ այս ուսուցչի դասարաններից
            var pupilGrades = new List<(string PupilUserId, int GradeId)>();
            if (datesOk && pupilIds.Count > 0)
            {
                var teacherGradeIds = await _db.TeacherGrades
                    .Where(tg => tg.TeacherUserId == CurrentUserId)
                    .Select(tg => tg.GradeId)
                    .ToListAsync();

                pupilGrades = (await _db.PupilGrades
                        .Where(pg => pupilIds.Contains(pg.PupilUserId) && teacherGradeIds.Contains(pg.GradeId))
                        .Select(pg => new { pg.PupilUserId, pg.GradeId })
                        .ToListAsync())
                    .GroupBy(p => p.PupilUserId)
                    .Select(g => (PupilUserId: g.Key, GradeId: g.First().GradeId))
                    .ToList();
            }

            if (!datesOk || pupilGrades.Count == 0)
            {
                var error = req.StartDate != default && req.DueDate != default && req.DueDate.Date < req.StartDate.Date
                    ? _l["DeadlineInvalid"].Value
                    : _l["AssignmentFormIncomplete"].Value;

                var vm = await BuildSendViewModel(id, error);
                if (vm == null) return NotFound();
                if (req.StartDate != default) vm.StartDate = req.StartDate.ToString("yyyy-MM-ddTHH:mm");
                if (req.DueDate != default) vm.DueDate = req.DueDate.ToString("yyyy-MM-ddTHH:mm");
                vm.PupilIds = pupilIds;
                return View("Form/Send", vm);
            }

            // Նոր շտեմարանի գրառում չի ստեղծվում՝ տրված առաջադրանքը կապվում է նույն բովանդակությանը
            var now = DateTime.Now;
            _db.GivenAssignments.Add(new GivenAssignment
            {
                AssignmentId = id,
                TeacherUserId = CurrentUserId,
                StartDate = req.StartDate,
                DueDate = req.DueDate,
                CreatedAt = now,
                Recipients = pupilGrades.Select(p => new AssignmentRecipient
                {
                    PupilUserId = p.PupilUserId,
                    GradeId = p.GradeId,
                    Status = AssignmentRecipientStatus.Sent,
                    SentAt = now
                }).ToList()
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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
                .FirstOrDefaultAsync(x => x.Id == id && x.TeacherUserId == CurrentUserId && x.IsInBank);
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

            var now = DateTime.Now;
            var storedNames = new List<string>();
            var removedFileIds = new List<int>();
            var (regionId, schoolId) = await TeacherLocationAsync();

            // Եթե այս շտեմարանի առաջադրանքը արդեն ուղարկվել է՝ ուղարկվածները ՉԵՆ փոխվում (ոչ մի կերպ)։
            // Փոփոխությունը պահվում է ՆՈՐ շտեմարանի առաջադրանքով, իսկ հինը թաքցվում է շտեմարանից,
            // բայց մնում է՝ արդեն տրված առաջադրանքների բովանդակությունը պահելու համար։
            var hasGiven = await _db.GivenAssignments.AnyAsync(g => g.AssignmentId == a.Id);
            var target = hasGiven
                ? new Assignment { TeacherUserId = CurrentUserId, IsInBank = true, CreatedAt = now }
                : a;

            try
            {
                var newFiles = new List<AssignmentFile>();
                foreach (var file in attachments)
                {
                    // assignments/{մարզ}/{դպրոց}/{ուսուցիչ}/{անուն}
                    var stored = await _files.SaveAsync(FileSections.Assignments, "teacher", CurrentUserId, regionId, schoolId, file);
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

                target.Title = req.Title.Trim();
                target.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
                target.TaskText = string.IsNullOrWhiteSpace(req.TaskText) ? null : req.TaskText.Trim();
                target.Types = types;
                target.OtherTypeText = types.HasFlag(AssignmentTypes.Other) ? req.OtherTypeText!.Trim() : null;
                target.GradingType = (AssignmentGradingType)req.GradingType;
                target.ResponseForms = forms;
                target.UpdatedAt = now;

                // առարկաներ՝ ջնջում ենք հանվածները, ավելացնում նորերը (նոր տարբերակի դեպքում՝ ավելացվում են բոլորը)
                _db.AssignmentSubjects.RemoveRange(target.Subjects.Where(s => !validSubjectIds.Contains(s.SubjectId)));
                foreach (var sid in validSubjectIds.Where(sid => target.Subjects.All(s => s.SubjectId != sid)))
                    target.Subjects.Add(new AssignmentSubject { SubjectId = sid });

                // դասարանների մակարդակներ
                _db.AssignmentGradeLevels.RemoveRange(target.GradeLevels.Where(g => !levels.Contains(g.Level)));
                foreach (var l in levels.Where(l => target.GradeLevels.All(g => g.Level != l)))
                    target.GradeLevels.Add(new AssignmentGradeLevel { Level = l });

                // ֆայլեր
                if (hasGiven)
                {
                    // նոր տարբերակը կապվում է թողնված (արդեն գոյություն ունեցող) ֆայլերին՝ առանց կրկնօրինակման
                    foreach (var f in a.Files.Where(f => keepIds.Contains(f.Id))) target.Files.Add(f);
                }
                else
                {
                    // հանվածների կապն է հանվում (ֆայլը կարող է օգտագործվել այլ առաջադրանքում)
                    var removedFiles = a.Files.Where(f => !keepIds.Contains(f.Id)).ToList();
                    foreach (var f in removedFiles) a.Files.Remove(f);
                    removedFileIds = removedFiles.Select(f => f.Id).ToList();
                }
                foreach (var f in newFiles) target.Files.Add(f);

                if (hasGiven)
                {
                    _db.Assignments.Add(target);
                    a.IsInBank = false;     // հին տարբերակը թաքցվում է շտեմարանից
                }

                await _db.SaveChangesAsync();
            }
            catch
            {
                foreach (var n in storedNames)
                    _files.Delete(n);
                throw;
            }

            // եթե հանված ֆայլը այլևս ոչ մի առաջադրանքի չի կցված՝ ջնջում ենք գրառումը և սկավառակի ֆայլը
            await DeleteOrphanFilesAsync(removedFileIds);

            return RedirectToAction(nameof(Bank));
        }


        public record DeleteBankAssignmentRequest(int Id);

        // Շտեմարանից ջնջում։ Եթե առաջադրանքը երբեք չի ուղարկվել՝ ջնջվում է։
        // Եթե տրված առաջադրանքներ կան՝ դրանք դեռ օգտագործում են այս բովանդակությունը, ուստի միայն թաքցվում է շտեմարանից։
        [HttpPost("Bank/Delete")]
        public async Task<IActionResult> BankDelete([FromBody] DeleteBankAssignmentRequest req)
        {
            var exists = await _db.Assignments
                .AnyAsync(a => a.Id == req.Id && a.TeacherUserId == CurrentUserId && a.IsInBank);
            if (!exists) return NotFound();

            if (await _db.GivenAssignments.AnyAsync(g => g.AssignmentId == req.Id))
            {
                await _db.Assignments
                    .Where(a => a.Id == req.Id && a.TeacherUserId == CurrentUserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsInBank, false));
                return Ok();
            }

            // ձևանմուշի ֆայլերը՝ ջնջելուց հետո կստուգենք, թե որոնք մնացին առանց առաջադրանքի
            var fileIds = await _db.Assignments
                .Where(a => a.Id == req.Id)
                .SelectMany(a => a.Files)
                .Select(f => f.Id)
                .ToListAsync();

            await _db.Assignments
                .Where(a => a.Id == req.Id && a.TeacherUserId == CurrentUserId)
                .ExecuteDeleteAsync();

            await DeleteOrphanFilesAsync(fileIds);
            return Ok();
        }

        public record ArchiveAssignmentRequest(int Id);

        // «Արխիվացնել»՝ ընթացիկ առաջադրանքը տեղափոխվում է «Արխիվացված»
        [HttpPost("Archive")]
        public async Task<IActionResult> Archive([FromBody] ArchiveAssignmentRequest req)
        {
            var affected = await _db.GivenAssignments
                .Where(a => a.Id == req.Id
                         && a.TeacherUserId == CurrentUserId
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

        // Ջնջում է այն ֆայլերի գրառումները և սկավառակի ֆայլերը, որոնք այլևս ոչ մի առաջադրանքի չեն կցված
        private async Task DeleteOrphanFilesAsync(List<int> fileIds)
        {
            if (fileIds.Count == 0) return;

            var orphans = await _db.AssignmentFiles
                .Where(f => fileIds.Contains(f.Id) && !f.Assignments.Any())
                .ToListAsync();
            if (orphans.Count == 0) return;

            _db.AssignmentFiles.RemoveRange(orphans);
            await _db.SaveChangesAsync();

            // սկավառակից՝ միայն բազայի փոփոխությունը հաջողվելուց հետո
            foreach (var o in orphans) _files.Delete(o.StoredFileName);
        }

        // Ուսուցչի դպրոցը՝ նրա վերջին ուսումնական տարվա դասարաններից։
        // Մարզի Id-ն դեռ չկա (կգա AuthServer-ից) → 0։ Երբ հայտնվի՝ միայն այստեղ կփոխվի։
        private async Task<(int RegionId, int SchoolId)> TeacherLocationAsync()
        {
            var schoolId = await _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentUserId)
                .OrderByDescending(tg => tg.AcademicYear)
                .Select(tg => tg.Grade.InstitutionId)
                .FirstOrDefaultAsync();

            return (0, schoolId);
        }

        // Միայն այն առարկաները, որոնք ուսուցիչը դասավանդում է
        private Task<List<int>> ValidTeacherSubjectIds(IEnumerable<int> requested)
        {
            var wanted = requested.Distinct().ToList();
            return _db.TeacherSubjects
                .Where(ts => ts.TeacherUserId == CurrentUserId && wanted.Contains(ts.SubjectId))
                .Select(ts => ts.SubjectId)
                .ToListAsync();
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
                    .Where(f => f.Assignments.Any(a => a.Id == req.SourceTemplateId.Value
                                                    && a.TeacherUserId == CurrentUserId
                                                    && a.IsInBank))
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
                .Where(m => m.UserId == me && m.Group.Type != GroupType.Parents)   // ծնողների խմբերը առայժմ պասիվ են
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
                .Where(x => x.Id == templateId && x.TeacherUserId == CurrentUserId && x.IsInBank)
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

            // շտեմարանի ձևանմուշից թողնված ֆայլերը՝ նույն ֆայլը կապվում է նոր առաջադրանքին (նոր գրառում չի ստեղծվում)
            var keptFiles = new List<AssignmentFile>();
            if (req.SourceTemplateId.HasValue && req.KeepFileIds.Count > 0)
            {
                keptFiles = await _db.AssignmentFiles
                    .Where(f => req.KeepFileIds.Contains(f.Id)
                             && f.Assignments.Any(a => a.Id == req.SourceTemplateId.Value
                                                    && a.TeacherUserId == CurrentUserId
                                                    && a.IsInBank))
                    .ToListAsync();
            }

            var storedFiles = new List<(string FileName, string StoredName, string ContentType, long Size)>();
            var (regionId, schoolId) = await TeacherLocationAsync();
            try
            {
                foreach (var file in attachments)
                {
                    // assignments/{մարզ}/{դպրոց}/{ուսուցիչ}/{անուն}
                    var stored = await _files.SaveAsync(FileSections.Assignments, "teacher", CurrentUserId, regionId, schoolId, file);

                    storedFiles.Add((Path.GetFileName(file.FileName), stored, file.ContentType, file.Length));
                }

                // ── պահպանում՝ մեկ ուղարկված առաջադրանք (շտեմարանը նույն գրառումներն են) ──
                var now = DateTime.Now;

                // նոր ֆայլերը ստեղծվում են ՄԵԿ անգամ՝ շտեմարանի օրինակը և ուղարկվածը կապվում են նույն գրառման հետ
                var attachedFiles = storedFiles.Select(f => new AssignmentFile
                {
                    FileName = f.FileName,
                    StoredFileName = f.StoredName,
                    ContentType = f.ContentType,
                    SizeBytes = f.Size,
                    UploadedAt = now
                }).Concat(keptFiles).ToList();

                static string? Norm(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                var description = Norm(req.Description);
                var taskText = Norm(req.TaskText);
                var otherTypeText = types.HasFlag(AssignmentTypes.Other) ? req.OtherTypeText!.Trim() : null;

                // Շտեմարանից ուղարկելիս՝ եթե բովանդակությունը չի փոխվել, տրված առաջադրանքը կապվում է
                // ՆՈՒՅՆ շտեմարանի առաջադրանքին (ոչինչ չի կրկնօրինակվում)։ Եթե ձևում ինչ-որ բան փոխվել է՝
                // փոփոխվածը դառնում է նոր շտեմարանի առաջադրանք, հինը մնում է անփոփոխ։
                Assignment? source = null;
                if (req.SourceTemplateId.HasValue)
                {
                    source = await _db.Assignments
                        .Include(x => x.Subjects)
                        .Include(x => x.GradeLevels)
                        .Include(x => x.Files)
                        .FirstOrDefaultAsync(x => x.Id == req.SourceTemplateId.Value
                                               && x.TeacherUserId == CurrentUserId
                                               && x.IsInBank);
                }

                var sameAsSource = source != null
                    && source.Title == req.Title.Trim()
                    && source.Description == description
                    && source.TaskText == taskText
                    && source.Types == types
                    && source.OtherTypeText == otherTypeText
                    && source.GradingType == (AssignmentGradingType)req.GradingType
                    && source.ResponseForms == forms
                    && source.Subjects.Select(x => x.SubjectId).OrderBy(x => x).SequenceEqual(validSubjectIds.OrderBy(x => x))
                    && source.GradeLevels.Select(x => x.Level).OrderBy(x => x).SequenceEqual(levels.OrderBy(x => x))
                    && storedFiles.Count == 0
                    && source.Files.Select(x => x.Id).OrderBy(x => x).SequenceEqual(keptFiles.Select(x => x.Id).OrderBy(x => x));

                var content = sameAsSource
                    ? source!
                    : new Assignment
                    {
                        TeacherUserId = CurrentUserId,
                        IsInBank = true,
                        Title = req.Title.Trim(),
                        Description = description,
                        TaskText = taskText,
                        Types = types,
                        OtherTypeText = otherTypeText,
                        GradingType = (AssignmentGradingType)req.GradingType,
                        ResponseForms = forms,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Subjects = validSubjectIds.Select(id => new AssignmentSubject { SubjectId = id }).ToList(),
                        GradeLevels = levels.Select(l => new AssignmentGradeLevel { Level = l }).ToList(),
                        Files = attachedFiles.ToList()
                    };

                var given = new GivenAssignment
                {
                    Assignment = content,
                    TeacherUserId = CurrentUserId,
                    StartDate = req.StartDate,
                    DueDate = req.DueDate,
                    CreatedAt = now,
                    Recipients = pupilGrades.Select(p => new AssignmentRecipient
                    {
                        PupilUserId = p.PupilUserId,
                        GradeId = p.GradeId,
                        Status = AssignmentRecipientStatus.Sent,
                        SentAt = now
                    }).ToList()
                };
                _db.GivenAssignments.Add(given);

                await _db.SaveChangesAsync();
            }
            catch
            {
                // բազայի սխալի դեպքում ջնջում ենք արդեն գրված ֆայլերը
                foreach (var f in storedFiles)
                    _files.Delete(f.StoredName);
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {

            // տրված առաջադրանք՝ բովանդակությունը վերցվում է կապված շտեմարանի առաջադրանքից
            var a = await _db.GivenAssignments
                .Where(x => x.Id == id && x.TeacherUserId == CurrentUserId)
                .Select(x => new
                {
                    x.Id,
                    x.Assignment.Title,
                    x.Assignment.Description,
                    x.Assignment.TaskText,
                    x.Assignment.Types,
                    x.Assignment.OtherTypeText,
                    x.Assignment.GradingType,
                    x.Assignment.ResponseForms,
                    x.StartDate,
                    x.DueDate,
                    x.IsArchived,
                    x.CreatedAt,
                    Subjects = x.Assignment.Subjects.Select(s => s.Subject.Name).ToList(),
                    Levels = x.Assignment.GradeLevels.Select(g => g.Level).OrderBy(l => l).ToList(),
                    Files = x.Assignment.Files.OrderBy(f => f.Id).Select(f => new { f.Id, f.FileName }).ToList(),
                    Recipients = x.Recipients.OrderBy(r => r.Id).Select(r => new
                    {
                        r.Id,
                        r.PupilUserId,
                        r.Status,
                        r.SubmittedAt,
                        r.AnswerText,
                        r.Score,
                        r.MaxScore,
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

            // ուսուցչի խմբերը՝ «Խումբ» ֆիլտրի և «Ավելացնել սովորողներ» պատուհանի համար
            var addedPupilIds = a.Recipients.Select(r => r.PupilUserId).ToList();
            var groupOptions = await BuildGroupOptions(classGroups, addedPupilIds);

            // սովորող → նրա խմբերի անունները (սովորողը կարող է լինել մի քանի խմբում)
            var groupNamesByPupil = groupOptions
                .SelectMany(g => g.Pupils.Select(p => new { p.UserId, GroupName = g.Name }))
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => x.Select(y => y.GroupName).Distinct().OrderBy(n => n).ToList());

            List<string> GroupsOf(string pupilUserId) =>
                groupNamesByPupil.TryGetValue(pupilUserId, out var names) ? names : new List<string>();

            var vm = new AssignmentDetailViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Subject = string.Join(", ", a.Subjects),
                Grade = string.Join(", ", a.Levels.Select(l => _l["GradeLevelFormat", l].Value)),
                Type = string.Join(", ", typeLabels),
                GradingType = GradingTypeLabel(a.GradingType),
                StartDate = a.StartDate,
                DueDate = a.DueDate,
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
                    GroupName = string.Join(", ", GroupsOf(r.PupilUserId)),
                    GroupNames = GroupsOf(r.PupilUserId),
                    SubmittedDate = r.SubmittedAt,
                    Status = r.Status switch
                    {
                        AssignmentRecipientStatus.Graded => "Graded",
                        AssignmentRecipientStatus.Completed => "Submitted",
                        _ => "Pending"
                    },
                    Score = r.Score,
                    MaxScore = a.GradingType == AssignmentGradingType.CurrentScore ? 10 : r.MaxScore,
                    ScoreFeedback = r.Feedback ?? "",
                    AnswerText = r.AnswerText ?? "",
                    GradingType = a.GradingType.ToString(),
                    IsReadOnly = a.IsArchived,
                    Files = r.Files.Select(f => new AssignmentFileViewModel
                    {
                        Name = f.FileName,
                        ViewUrl = Url.Action(nameof(SubmissionFile), new { id = f.Id })!,
                        DownloadUrl = Url.Action(nameof(SubmissionFile), new { id = f.Id, download = true })!
                    }).ToList()
                }).ToList()
            };

            vm.AvailableGrades = classGroups;
            vm.AlreadyAddedPupilUserIds = addedPupilIds;
            vm.Groups = groupOptions;

            return View("Given/Detail", vm);
        }

        // Ուսուցչի կցած ֆայլի բացում (նոր էջում) կամ ներբեռնում
        [HttpGet("Files/{id:int}")]
        public async Task<IActionResult> AssignmentFile(int id, bool download = false)
        {
            var f = await _db.AssignmentFiles
                .Where(x => x.Id == id && x.Assignments.Any(a => a.TeacherUserId == CurrentUserId))
                .Select(x => new { x.FileName, x.StoredFileName, x.ContentType })
                .FirstOrDefaultAsync();

            if (f == null) return NotFound();

            var stream = _files.OpenRead(f.StoredFileName);
            if (stream == null) return NotFound();

            var contentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType;

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
                .Where(x => x.Id == id && x.Recipient.GivenAssignment.TeacherUserId == CurrentUserId)
                .Select(x => new { x.FileName, x.StoredFileName, x.ContentType })
                .FirstOrDefaultAsync();

            if (f == null) return NotFound();

            var stream = _files.OpenRead(f.StoredFileName);
            if (stream == null) return NotFound();

            var contentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType;

            return download
                ? File(stream, contentType, f.FileName)
                : File(stream, contentType);
        }

        public class SaveGradeRequest
        {
            public int SubmissionId { get; set; }   // AssignmentRecipient.Id
            public decimal? Score { get; set; }

            /// <summary>Միայն հայտորոշիչ գնահատման համար՝ «Առավելագույն միավոր»</summary>
            public decimal? MaxScore { get; set; }
            public string? Feedback { get; set; }
        }

        // Գնահատում՝ ըստ առաջադրանքի գնահատման տեսակի
        [HttpPost("Grade")]
        public async Task<IActionResult> SaveGrade([FromBody] SaveGradeRequest req)
        {
            var r = await _db.AssignmentRecipients
                .Include(x => x.GivenAssignment).ThenInclude(g => g.Assignment)
                .FirstOrDefaultAsync(x => x.Id == req.SubmissionId
                                       && x.GivenAssignment.TeacherUserId == CurrentUserId
                                       && !x.GivenAssignment.IsArchived);
            if (r == null) return NotFound();

            var feedback = string.IsNullOrWhiteSpace(req.Feedback) ? null : req.Feedback.Trim();
            if (feedback is { Length: > 2000 }) return BadRequest();

            switch (r.GivenAssignment.Assignment.GradingType)
            {
                case AssignmentGradingType.CurrentScore:
                    // միավորը (1–10) և/կամ հետադարձ կապ՝ առնվազն մեկը
                    if (req.Score is not null
                        && (req.Score < 1 || req.Score > 10 || req.Score != decimal.Truncate(req.Score.Value)))
                        return BadRequest();
                    if (req.Score is null && feedback is null) return BadRequest();
                    r.Score = req.Score;
                    r.MaxScore = null;
                    break;

                case AssignmentGradingType.Diagnostic:
                    // «Առավելագույն միավոր» և «Նախնական միավոր» (ազատ թվեր) և/կամ հետադարձ կապ՝ առնվազն մեկը
                    if (req.MaxScore is not null && (req.MaxScore <= 0 || req.MaxScore > 9999))
                        return BadRequest();
                    if (req.Score is not null && (req.Score < 0 || req.Score > 9999))
                        return BadRequest();
                    // նախնական միավորը չի կարող գերազանցել առավելագույնը
                    if (req.Score is not null && req.MaxScore is not null && req.Score > req.MaxScore)
                        return BadRequest();
                    if (req.Score is null && req.MaxScore is null && feedback is null)
                        return BadRequest();
                    r.Score = req.Score is null ? null : Math.Round(req.Score.Value, 2);
                    r.MaxScore = req.MaxScore is null ? null : Math.Round(req.MaxScore.Value, 2);
                    break;

                default: // Formative՝ միայն հետադարձ կապ
                    if (feedback is null) return BadRequest();
                    r.Score = null;
                    r.MaxScore = null;
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
            var assignment = await _db.GivenAssignments
                .Where(x => x.Id == req.AssignmentId
                         && x.TeacherUserId == CurrentUserId
                         && !x.IsArchived)
                .Select(x => new { x.Id })
                .FirstOrDefaultAsync();
            if (assignment == null) return NotFound();

            var wanted = req.PupilIds.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (wanted.Count == 0) return BadRequest();

            var existing = await _db.AssignmentRecipients
                .Where(r => r.GivenAssignmentId == assignment.Id && wanted.Contains(r.PupilUserId))
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
                    GivenAssignmentId = assignment.Id,
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
            var a = await _db.GivenAssignments
                .Where(x => x.Id == req.Id && x.TeacherUserId == CurrentUserId && !x.IsArchived)
                .Select(x => new { x.StartDate })
                .FirstOrDefaultAsync();
            if (a == null) return NotFound();

            if (req.DueDate.Date < a.StartDate.Date) return BadRequest();

            await _db.GivenAssignments
                .Where(x => x.Id == req.Id && x.TeacherUserId == CurrentUserId && !x.IsArchived)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.DueDate, req.DueDate));

            return Ok();
        }
    }
}