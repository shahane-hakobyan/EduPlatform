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
    /// «Խմբեր»՝ ընդհանուր բաժին (ուսուցիչ, հետագայում նաև սովորող)։ Հասցեները՝ /Groups/...
    /// Էջերը՝ Views/Shared/Groups/{Index|Create|Detail|Edit}.cshtml
    /// </summary>
    [Authorize(Roles = "Teacher")]
    [Route("Groups")]
    public class GroupsController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly EduPlatformDbContext _db;
        private readonly IConfiguration _config;
        private readonly IClassRosterService _roster;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public GroupsController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer,
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
            ViewBag.SidebarItems = _menuBuilder.ForUser(User);
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForUser(User);
            base.OnActionExecuting(context);
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
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {

            var rows = await _db.UserGroupMembers
                .Where(m => m.UserId == CurrentUserId)
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
                ImageUrl = r.ImageStoredFileName == null ? null : Url.Action(nameof(Image), new { id = r.Id }),
                TypeKey = r.Type.ToString(),
                TypeLabel = GroupTypeLabel(r.Type),
                IsAdmin = r.Role == GroupMemberRole.Admin,
                RoleLabel = r.Role == GroupMemberRole.Admin ? _l["GroupRoleAdmin"].Value : _l["GroupRoleMember"].Value,
                RoleShortLabel = r.Role == GroupMemberRole.Admin ? _l["GroupRoleAdminShort"].Value : _l["GroupRoleMemberShort"].Value,
                MemberCount = r.MemberCount,
                DetailUrl = Url.Action(nameof(Detail), new { id = r.Id })!
            }).ToList();

            var filters = new List<MaterialFilterOption>
            {
                new()
                {
                    Key = "type", Label = _l["GroupTypeLabel"],
                    Options = new() { GroupTypeLabel(GroupType.Pupils), GroupTypeLabel(GroupType.Other) }
                },
                new()
                {
                    Key = "role", Label = _l["MyRoleInGroup"],
                    Options = new() { _l["GroupRoleAdmin"].Value, _l["GroupRoleMember"].Value }
                }
            };

            return View("Index", new GroupsPageViewModel
            {
                Groups = cards,
                Filters = filters,
                CreateUrl = Url.Action(nameof(Create))!
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
                .Where(tg => tg.TeacherUserId == CurrentUserId)
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
                if (userId == CurrentUserId) return _l["YouLabel"].Value;

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

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {

            return View("Create", await BuildCreateGroupViewModel());
        }

        private async Task<CreateGroupViewModel> BuildCreateGroupViewModel(CreateGroupRequest? req = null, string? error = null) => new()
        {
            ClassGroups = await BuildClassGroups(),
            SubmitUrl = Url.Action(nameof(Create))!,
            CancelUrl = Url.Action(nameof(Index))!,
            Error = error,
            Name = req?.Name ?? "",
            Type = req != null && Enum.IsDefined(typeof(GroupType), req.Type) ? req.Type : (int)GroupType.Pupils
        };

        [HttpPost("Create")]
        [RequestSizeLimit(10_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
        public async Task<IActionResult> Create(CreateGroupRequest req)
        {
            var name = (req.Name ?? "").Trim();
            // Ծնողների խումբ՝ առայժմ չի թույլատրվում (այս փուլում ծնողներ չկան)
            var typeValid = Enum.IsDefined(typeof(GroupType), req.Type) && req.Type != (int)GroupType.Parents;
            var image = req.Image is { Length: > 0 } ? req.Image : null;

            string? error = null;
            if (name.Length == 0 || name.Length > 200 || !typeValid)
                error = _l["GroupFormIncomplete"].Value;
            else if (image != null && !IsValidGroupImage(image))
                error = _l["GroupImageInvalid"].Value;

            if (error != null)
            {
                return View("Create", await BuildCreateGroupViewModel(req, error));
            }

            var type = (GroupType)req.Type;
            var now = DateTime.Now;

            // անդամներ՝ սովորողների և ծնողների խմբում (միշտ ուսուցչի դասարանների սովորողներից)
            var memberIds = await ResolveGroupMemberIds(type, req.MemberIds);

            var group = new UserGroup
            {
                Name = name,
                Type = type,
                CreatedByUserId = CurrentUserId,
                CreatedAt = now,
                UpdatedAt = now,
                Members = new List<UserGroupMember>
                {
                    new() { UserId = CurrentUserId, Role = GroupMemberRole.Admin, AddedAt = now }
                }
            };
            foreach (var id in memberIds.Where(id => id != CurrentUserId))
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

            return RedirectToAction(nameof(Index));
        }

        // Խմբի էջ՝ տեսնում են միայն խմբի անդամները
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {

            var g = await _db.UserGroups
                .Where(x => x.Id == id && x.Members.Any(m => m.UserId == CurrentUserId))
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
                IsMe = m.UserId == CurrentUserId,
                RoleLabel = m.Role == GroupMemberRole.Admin ? _l["GroupRoleAdminShort"].Value : _l["GroupRoleMemberShort"].Value,
                ProfileUrl = "#"
            }).ToList();

            return View("Detail", new GroupDetailViewModel
            {
                Id = g.Id,
                Name = g.Name,
                ImageUrl = g.ImageStoredFileName == null ? null : Url.Action(nameof(Image), new { id = g.Id }),
                TypeKey = g.Type.ToString(),
                TypeLabel = GroupTypeLabel(g.Type),
                MemberCount = members.Count,
                IsAdmin = g.Members.Any(m => m.UserId == CurrentUserId && m.Role == GroupMemberRole.Admin),
                Members = members,
                BackUrl = Url.Action(nameof(Index))!,
                EditUrl = Url.Action(nameof(Edit), new { id = g.Id })!
            });
        }

        // ── Խմբի խմբագրում՝ միայն ադմինը ───────────────────────────────────────────

        private async Task<EditGroupViewModel?> BuildEditGroupViewModel(int id, EditGroupRequest? req = null, string? error = null)
        {
            var g = await _db.UserGroups
                .Where(x => x.Id == id
                         && x.Members.Any(m => m.UserId == CurrentUserId && m.Role == GroupMemberRole.Admin))
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
                ImageUrl = g.ImageStoredFileName == null ? null : Url.Action(nameof(Image), new { id = g.Id }),
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
                SubmitUrl = Url.Action(nameof(Edit), new { id = g.Id })!,
                CancelUrl = Url.Action(nameof(Detail), new { id = g.Id })!,
                Error = error
            };
        }

        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(int id)
        {

            var vm = await BuildEditGroupViewModel(id);
            if (vm == null) return NotFound();

            return View("Edit", vm);
        }

        [HttpPost("{id:int}/Edit")]
        [RequestSizeLimit(10_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
        public async Task<IActionResult> Edit(int id, EditGroupRequest req)
        {
            var g = await _db.UserGroups
                .Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.Id == id
                                       && x.Members.Any(m => m.UserId == CurrentUserId && m.Role == GroupMemberRole.Admin));
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
                var vm = await BuildEditGroupViewModel(id, req, error);
                return View("Edit", vm);
            }

            var now = DateTime.Now;

            // անդամների հեռացում (ադմինին՝ ստեղծողին, հեռացնել չի կարելի)
            var removeIds = req.RemoveMemberIds.ToHashSet();
            _db.UserGroupMembers.RemoveRange(g.Members.Where(m => m.Role != GroupMemberRole.Admin && removeIds.Contains(m.UserId)));

            // նոր անդամներ (խմբի տեսակը չի փոխվում)
            var existing = g.Members.Select(m => m.UserId).ToHashSet();
            foreach (var uid in (await ResolveGroupMemberIds(g.Type, req.AddMemberIds)).Where(u => !existing.Contains(u) && u != CurrentUserId))
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

            return RedirectToAction(nameof(Detail), new { id });
        }

        public record DeleteGroupRequest(int Id);

        // Խմբի ջնջում՝ միայն ադմինը
        [HttpPost("Delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteGroupRequest req)
        {
            var g = await _db.UserGroups
                .Where(x => x.Id == req.Id
                         && x.Members.Any(m => m.UserId == CurrentUserId && m.Role == GroupMemberRole.Admin))
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
        [HttpGet("{id:int}/Image")]
        public async Task<IActionResult> Image(int id)
        {
            var g = await _db.UserGroups
                .Where(x => x.Id == id && x.Members.Any(m => m.UserId == CurrentUserId))
                .Select(x => new { x.ImageStoredFileName, x.ImageContentType })
                .FirstOrDefaultAsync();

            if (g == null || string.IsNullOrEmpty(g.ImageStoredFileName)) return NotFound();

            var path = Path.Combine(GroupImagesRoot(), Path.GetFileName(g.ImageStoredFileName));
            if (!System.IO.File.Exists(path)) return NotFound();

            return File(System.IO.File.OpenRead(path),
                string.IsNullOrWhiteSpace(g.ImageContentType) ? "application/octet-stream" : g.ImageContentType);
        }

        // Ուսուցչի դասարանները և նրանց աշակերտները՝ IClassRosterService-ից
        private Task<List<AssignmentClassGroupViewModel>> BuildClassGroups()
            => _roster.BuildClassGroupsAsync(CurrentUserId);
    }
}
