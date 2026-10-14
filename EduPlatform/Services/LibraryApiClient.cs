using System.Text.Json;

namespace EduPlatform.Services
{
    /// <summary>Library.Api-ի <c>BookDto</c>-ի արտացոլումը:</summary>
    public class LibraryBookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string MaterialType { get; set; } = "";
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
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;

        public LibraryApiClient(HttpClient http) => _http = http;

        public async Task<List<LibraryBookDto>?> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new();

            try
            {
                var query = string.Join("&", idList.Select(i => $"ids={i}"));
                var res = await _http.GetAsync($"api/books?{query}");
                if (!res.IsSuccessStatusCode) return null;
                return await res.Content.ReadFromJsonAsync<List<LibraryBookDto>>(Json);
            }
            catch (HttpRequestException) { return null; }
            catch (TaskCanceledException) { return null; }
        }
    }
}
