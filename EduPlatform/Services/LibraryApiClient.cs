using System.Text.Json;

namespace EduPlatform.Services
{
    /// <summary>Library.Api-ի <c>EduMaterialDto</c>-ի արտացոլումը:</summary>
    public class LibraryBookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string MaterialType { get; set; } = "";

        /// <summary>OtherBook-ի համար՝ Textbook, Fiction կամ PopularScience:</summary>
        public string? Category { get; set; }

        /// <summary>OtherMaterial-ի համար՝ Text, Video, Audio, Image, ...:</summary>
        public string? Kind { get; set; }

        public string PublisherName { get; set; } = "";
        public int? PublishedYear { get; set; }
        public List<string> Authors { get; set; } = new();
        public List<string> Subjects { get; set; } = new();
        public List<int> Grades { get; set; } = new();
        public string? CoverUrl { get; set; }
        public string? ViewUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public int? Pages { get; set; }
        public string? Language { get; set; }

        /// <summary>Guaranteed, NotGuaranteed կամ Archived (Library.Api-ի EduMaterialDto.Status)</summary>
        public string? Status { get; set; }
    }

    /// <summary>Library.Api-ի by-ids պատասխանը:</summary>
    public class LibraryByIdsResult
    {
        /// <summary>Ցուցադրվող նյութերը:</summary>
        public List<LibraryBookDto> Items { get; set; } = new();

        /// <summary>
        /// Library-ում ԻՍԿԱՊԵՍ ջնջված նյութերի id-ները (Library-ն է հաստատում)։
        /// Միայն սրանք են հեռացվում «Իմ գրադարան»-ից։
        /// Թաքցված / չերաշխավորված նյութերն այստեղ չեն լինում։
        /// </summary>
        public List<int> DeletedIds { get; set; } = new();
    }

    public interface ILibraryApiClient
    {
        /// <returns>
        /// Ցուցադրվող նյութերը և ջնջվածների id-ները,
        /// կամ null՝ եթե Library-ն անհասանելի է (այդ դեպքում ոչինչ չհեռացնել):
        /// </returns>
        Task<LibraryByIdsResult?> GetByIdsAsync(IEnumerable<int> ids);
    }

    public class LibraryApiClient : ILibraryApiClient
    {
        // Library.Api-ն մեկ հարցումով ընդունում է առավելագույնը 200 id
        private const int BatchSize = 200;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;
        private readonly ILogger<LibraryApiClient> _log;

        public LibraryApiClient(HttpClient http, ILogger<LibraryApiClient> log)
        {
            _http = http;
            _log = log;
        }

        public async Task<LibraryByIdsResult?> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            var result = new LibraryByIdsResult();
            if (idList.Count == 0) return result;

            try
            {
                foreach (var batch in idList.Chunk(BatchSize))
                {
                    var res = await _http.PostAsJsonAsync("api/edu-materials/by-ids", new { ids = batch });
                    if (!res.IsSuccessStatusCode)
                    {
                        _log.LogWarning("Library.Api by-ids returned {Status}", (int)res.StatusCode);
                        return null;
                    }

                    var part = await ReadResultAsync(res);
                    if (part is null) return null;

                    result.Items.AddRange(part.Items);

                    // պաշտպանություն՝ ընդունում ենք միայն այն id-ները, որ մենք ենք հարցրել
                    result.DeletedIds.AddRange(part.DeletedIds.Where(batch.Contains));
                }

                return result;
            }
            catch (HttpRequestException ex) { _log.LogWarning(ex, "Library.Api unreachable"); return null; }
            catch (TaskCanceledException ex) { _log.LogWarning(ex, "Library.Api timeout"); return null; }
            catch (JsonException ex) { _log.LogWarning(ex, "Library.Api returned invalid JSON"); return null; }
        }

        /// <summary>
        /// Աջակցում է երկու ձևաչափի.
        /// նոր՝ <c>{ items: [...], deletedIds: [...] }</c>,
        /// հին՝ <c>[...]</c> (այդ դեպքում DeletedIds-ը դատարկ է → ոչինչ չի ջնջվում)։
        /// </summary>
        private static async Task<LibraryByIdsResult?> ReadResultAsync(HttpResponseMessage res)
        {
            var root = await res.Content.ReadFromJsonAsync<JsonElement>(Json);

            return root.ValueKind switch
            {
                JsonValueKind.Object => root.Deserialize<LibraryByIdsResult>(Json),
                JsonValueKind.Array => new LibraryByIdsResult
                {
                    Items = root.Deserialize<List<LibraryBookDto>>(Json) ?? new()
                },
                _ => null
            };
        }
    }
}