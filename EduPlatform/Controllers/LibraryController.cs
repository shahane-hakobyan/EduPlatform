using EduPlatform.Data;
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
    /// «Իմ գրադարան»՝ օգտատիրոջ պահած նյութերը (Library-ից)։ Ընդհանուր բաժին՝ ուսուցիչ և սովորող։
    /// Հասցեները՝ /Library/Materials/{Textbooks|MethodologicalGuidelines|OtherEducationalMaterials}, /Library/OtherBooks
    /// </summary>
    [Authorize]
    [Route("Library")]
    public class LibraryController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly EduPlatformDbContext _db;
        private readonly ILibraryApiClient _library;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public LibraryController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer,
            EduPlatformDbContext db, ILibraryApiClient library)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
            _library = library;
        }

        // Կողային մենյուն ամեն էջի համար
        // TODO (հաջորդ քայլ)՝ սովորողի համար՝ ForPupil()
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
            base.OnActionExecuting(context);
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
                .Where(x => x.UserId == CurrentUserId && x.MaterialType == type)
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

        [HttpGet("Materials/{id}")]
        public async Task<IActionResult> Materials(string id)
        {

            if (LibraryTypeByRoute.ContainsKey(id))
            {
                var page = await BuildSavedMaterialsPage(id);
                return View(page);
            }

            return NotFound();
        }

        public record RemoveSavedRequest(string MaterialType, int Id);

        // «Իմ գրադարան»-ից նյութի հեռացում (Library-ի նյութը չի ջնջվում, միայն այս օգտատիրոջ պահվածը)
        [HttpPost("RemoveSavedMaterial")]
        public async Task<IActionResult> RemoveSavedMaterial([FromBody] RemoveSavedRequest req)
        {
            await _db.UserLibraryMaterials
                .Where(x => x.UserId == CurrentUserId
                         && x.MaterialType == req.MaterialType
                         && x.LibraryMaterialId == req.Id)
                .ExecuteDeleteAsync();

            return Ok();
        }

        [HttpGet("OtherBooks")]
        public async Task<IActionResult> OtherBooks()
        {

            var saved = await _db.UserLibraryMaterials
                .Where(x => x.UserId == CurrentUserId && x.MaterialType == "OtherBook")
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

            return View(tabs);
        }
    }
}
