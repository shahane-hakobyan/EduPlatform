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
        public int PublishedYear { get; set; }
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

    public interface ILibraryApiClient
    {
        /// <returns>
        /// Library-ում դեռ գոյություն ունեցող նյութերը,
        /// կամ null՝ եթե Library-ն անհասանելի է (այդ դեպքում ոչինչ չհեռացնել):
        /// </returns>
        Task<List<LibraryBookDto>?> GetByIdsAsync(IEnumerable<int> ids);
    }

    public class LibraryApiClient : ILibraryApiClient
    {
        // Library.Api-ն մեկ հարցումով ընդունում է առավելագույնը 200 id
        private const int BatchSize = 200;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;

        public LibraryApiClient(HttpClient http) => _http = http;

        public async Task<List<LibraryBookDto>?> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new();

            var result = new List<LibraryBookDto>();

            try
            {
                foreach (var batch in idList.Chunk(BatchSize))
                {
                    var res = await _http.PostAsJsonAsync("api/edu-materials/by-ids", new { ids = batch });
                    if (!res.IsSuccessStatusCode) return null;

                    var items = await res.Content.ReadFromJsonAsync<List<LibraryBookDto>>(Json);
                    if (items is null) return null;
                    result.AddRange(items);
                }

                return result;
            }
            catch (HttpRequestException) { return null; }
            catch (TaskCanceledException) { return null; }
        }
    }
}
