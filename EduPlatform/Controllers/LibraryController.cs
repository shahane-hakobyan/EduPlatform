using EduPlatform.Data;
using EduPlatform.Data.Models;
using EduPlatform.Resources;
using EduPlatform.Services;
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

        private const string OtherBookType = "OtherBook";

        private static readonly string[] CardColors =
            { "blue", "purple", "green", "orange", "pink", "red", "teal", "emerald", "indigo", "amber" };

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

        /// <summary>Library-ի կարգավիճակը → «Գործող» (Երաշխավորված) / «Արխիվացված»։ Չերաշխավորվածը ֆիլտրում չի երևում։</summary>
        private string StatusLabel(string? status) => status switch
        {
            "Guaranteed" => _l["StatusActive"].Value,
            "Archived" => _l["StatusArchived"].Value,
            _ => ""
        };

        /// <summary>Ֆիլտրերի ընտրելի արժեքները կազմվում են էջի նյութերից:</summary>
        private List<MaterialFilterOption> BuildFilters(IReadOnlyCollection<MaterialCardViewModel> cards, params string[] keys)
        {
            var result = new List<MaterialFilterOption>();
            var activeLabel = _l["StatusActive"].Value;

            foreach (var key in keys)
            {
                string label;
                IEnumerable<string?> values;

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
                        label = _l["PublicationYear"]; values = cards.Select(c => c.Year?.ToString()); break;
                    case "publisher":
                        label = _l["Publisher"]; values = cards.Select(c => c.Publisher); break;
                    case "language":
                        label = _l["Language"]; values = cards.Select(c => c.Language); break;
                    case "type":
                        label = _l["MaterialType"]; values = cards.Select(c => c.TypeLabel); break;
                    case "status":
                        label = _l["StatusFilter"]; values = cards.Select(c => c.StatusLabel); break;
                    default:
                        continue;
                }

                var options = values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!)
                    .Distinct()
                    .OrderBy(v => int.TryParse(v, out var n) ? n : int.MaxValue)
                    .ThenBy(v => v == activeLabel ? 0 : 1)   // կարգավիճակում «Գործող»-ը առաջինը
                    .ThenBy(v => v)
                    .ToList();

                if (options.Count > 0)
                    result.Add(new MaterialFilterOption { Key = key, Label = label, Options = options });
            }

            return result;
        }

        /// <summary>
        /// Օգտատիրոջ տվյալ տեսակի պահվածները (նորերը՝ առաջինը)՝ Library.Api-ի տվյալներով։
        /// Library-ում ջնջված նյութերը հեռացվում են մեր աղյուսակից։
        /// Library-ն անհասանելի է → ոչինչ չենք հեռացնում, ցուցակը դատարկ է։
        /// </summary>
        private async Task<List<(UserLibraryMaterial Saved, LibraryBookDto Book)>> LoadSavedAsync(string materialType)
        {
            var saved = await _db.UserLibraryMaterials
                .Where(x => x.UserId == CurrentUserId && x.MaterialType == materialType)
                .OrderByDescending(x => x.SavedAt)
                .ToListAsync();

            var books = await _library.GetByIdsAsync(saved.Select(s => s.LibraryMaterialId));
            if (books is null) return new();

            var byId = books.ToDictionary(b => b.Id);

            var orphans = saved.Where(s => !byId.ContainsKey(s.LibraryMaterialId)).ToList();
            if (orphans.Count > 0)
            {
                _db.UserLibraryMaterials.RemoveRange(orphans);
                await _db.SaveChangesAsync();
            }

            return saved
                .Where(s => byId.ContainsKey(s.LibraryMaterialId))
                .Select(s => (s, byId[s.LibraryMaterialId]))
                .ToList();
        }

        /// <summary>
        /// Մեկ քարտի տվյալները։ MaterialType-ը վերցվում է մեր աղյուսակից, որ հեռացնելիս անպայման համընկնի։
        /// <paramref name="withKindAndStatus"/>՝ տեսակ և կարգավիճակ (այլ գրքերի համար չեն ցուցադրվում)։
        /// </summary>
        private MaterialCardViewModel ToCard(UserLibraryMaterial saved, LibraryBookDto b, bool withKindAndStatus = true) => new()
        {
            Id = b.Id,
            Title = b.Title,
            MaterialType = saved.MaterialType,
            TypeLabel = withKindAndStatus ? KindLabel(b.Kind) : null,
            Grades = b.Grades.OrderBy(g => g).Select(g => g.ToString()).ToList(),
            Subjects = b.Subjects,
            ColorVariant = CardColors[Math.Abs(b.Id) % CardColors.Length],
            ThumbnailUrl = b.CoverUrl,
            Authors = string.Join(", ", b.Authors),
            Publisher = b.PublisherName,
            Language = b.Language,
            Pages = b.Pages ?? 0,
            Year = b.PublishedYear is > 0 ? b.PublishedYear : null,
            ViewUrl = b.ViewUrl ?? "",
            DownloadUrl = b.DownloadUrl ?? "",
            StatusLabel = withKindAndStatus ? StatusLabel(b.Status) : "",
            IsArchived = withKindAndStatus && b.Status == "Archived"
        };

        private MaterialsPageViewModel PageOf(string title, List<MaterialCardViewModel> cards, params string[] filterKeys) => new()
        {
            Title = title,
            TotalCount = cards.Count,
            Filters = BuildFilters(cards, filterKeys),
            Materials = cards
        };

        [HttpGet("Materials/{id}")]
        public async Task<IActionResult> Materials(string id)
        {
            if (!LibraryTypeByRoute.TryGetValue(id, out var type))
                return NotFound();

            var cards = (await LoadSavedAsync(type)).Select(x => ToCard(x.Saved, x.Book)).ToList();

            if (id == "OtherEducationalMaterials")
                return View(PageOf(_l[id], cards, "subject", "grade", "author", "year", "publisher", "type"));

            // Դասագրքեր և մեթոդական ուղեցույցներ՝ սկզբում գործողները, ապա արխիվացվածները, ներսում՝ ըստ դասարանի
            static int FirstGrade(MaterialCardViewModel m)
                => m.Grades.Count > 0 && int.TryParse(m.Grades[0], out var g) ? g : int.MaxValue;

            cards = cards
                .OrderBy(m => m.IsArchived ? 1 : 0)
                .ThenBy(FirstGrade)
                .ThenBy(m => m.Title)
                .ToList();

            return View(PageOf(_l[id], cards, "subject", "grade", "author", "year", "publisher", "status"));
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
            var saved = await LoadSavedAsync(OtherBookType);

            MaterialsPageViewModel Tab(string category, string title, params string[] filterKeys) =>
                PageOf(title,
                    saved.Where(x => x.Book.Category == category)
                         .Select(x => ToCard(x.Saved, x.Book, withKindAndStatus: false))
                         .ToList(),
                    filterKeys);

            var tabs = new List<OtherBooksTabViewModel>
            {
                new() { Key = "other-textbooks", Content = Tab("Textbook", _l["OtherTextbooks"], "subject", "grade", "author", "year", "publisher", "language") },
                new() { Key = "fiction", Content = Tab("Fiction", _l["FictionBooks"], "author", "year", "publisher", "language") },
                new() { Key = "popular-science", Content = Tab("PopularScience", _l["PopularScienceBooks"], "author", "year", "publisher", "language") },
            };

            return View(tabs);
        }
    }
}
