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

        // Ուսուցչի դասարանները և նրանց աշակերտները՝ IClassRosterService-ից (նույնն օգտագործում են նաև առաջադրանքները)
        private Task<List<AssignmentClassGroupViewModel>> BuildClassGroups()
            => _roster.BuildClassGroupsAsync(CurrentTeacherUserId);
    }
}
