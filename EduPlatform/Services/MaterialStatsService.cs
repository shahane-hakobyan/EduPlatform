using EduPlatform.ViewModels.Statistics;
using System.Text.Json;

namespace EduPlatform.Services
{
    public interface IMaterialStatsService
    {
        /// <summary>
        /// Օգտատիրոջ նյութերի վիճակագրությունը՝ բոլոր գործիքներից միավորված։
        /// <paramref name="period"/>՝ 7d, month, 3m, year։
        /// <paramref name="type"/>՝ "" (բոլորը), folder, digitized, crossword։
        /// </summary>
        Task<(MaterialStatsDto Stats, int FailedSources)> GetCombinedAsync(string userId, string period, string type);
    }

    /// <summary>
    /// Նույն տրամաբանությունն է, ինչ AuthServer-ի StatsService-ը.
    /// հարցումներն ուղարկվում են զուգահեռ, արդյունքները գումարվում են։
    /// Հասցեները՝ MaterialStatsApis, բանալիները՝ MaterialStatsApiKeys (user-secrets / IIS, ոչ git)։
    /// Բանալի չունեցող կամ չպատասխանող աղբյուրը բաց է թողնվում՝ էջը միևնույն է բացվում է։
    /// </summary>
    public class MaterialStatsService : IMaterialStatsService
    {
        private const int TopCount = 6;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<MaterialStatsService> _log;

        public MaterialStatsService(HttpClient http, IConfiguration config, ILogger<MaterialStatsService> log)
        {
            _http = http;
            _config = config;
            _log = log;
        }

        public async Task<(MaterialStatsDto Stats, int FailedSources)> GetCombinedAsync(string userId, string period, string type)
        {
            var id = Uri.EscapeDataString(userId);
            var tasks = new List<Task<MaterialStatsDto?>>();
            var all = string.IsNullOrEmpty(type);

            // IBook՝ 1 = թղթապանակներ, 2 = թվայնացված նյութեր
            if (all || type == "folder")
                tasks.Add(FetchAsync("IBook", $"ibook/api/Statistics/type/1/{id}/{period}"));
            if (all || type == "digitized")
                tasks.Add(FetchAsync("IBook", $"ibook/api/Statistics/type/2/{id}/{period}"));

            if (all || type == "crossword")
                tasks.Add(FetchAsync("Crossword", $"api/users/{id}/stats/{period}"));

            // «Բոլորը»-ում՝ նաև զույգերի խաղը և հարցաշտեմարանը (ինչպես AuthServer-ում)
            if (all)
            {
                tasks.Add(FetchAsync("PairsGame", $"api/users/{id}/stats/{period}"));
                tasks.Add(FetchAsync("QuestionBank", $"api/users/{id}/stats/{period}"));
            }

            var results = await Task.WhenAll(tasks);
            var failed = results.Count(r => r is null);

            return (Combine(results.Where(r => r is not null)!), failed);
        }

        // Մեկ աղբյուր. null՝ եթե կարգավորված չէ կամ չպատասխանեց
        private async Task<MaterialStatsDto?> FetchAsync(string source, string path)
        {
            var baseUrl = _config[$"MaterialStatsApis:{source}"];
            var apiKey = _config[$"MaterialStatsApiKeys:{source}"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                _log.LogWarning("Material stats: {Source} is not configured (URL or API key missing)", source);
                return null;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/{path}");
                request.Headers.Add("X-Api-Key", apiKey);

                using var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("Material stats: {Source} returned {Status}", source, (int)response.StatusCode);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<MaterialStatsDto>(Json) ?? new MaterialStatsDto();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _log.LogWarning(ex, "Material stats: {Source} failed", source);
                return null;
            }
        }

        // Բոլոր աղբյուրների արդյունքը՝ մեկ օբյեկտում
        private static MaterialStatsDto Combine(IEnumerable<MaterialStatsDto> sources)
        {
            var result = new MaterialStatsDto
            {
                MonthlyViews = Enumerable.Repeat(0, 12).ToList(),
                MonthlyLikes = Enumerable.Repeat(0, 12).ToList(),
            };

            foreach (var s in sources)
            {
                result.TotalMaterials += s.TotalMaterials;
                result.TotalViews += s.TotalViews;
                result.TotalLikes += s.TotalLikes;
                result.TotalShares += s.TotalShares;
                result.PreviousPeriodViews += s.PreviousPeriodViews;
                result.PreviousPeriodLikes += s.PreviousPeriodLikes;
                result.PreviousPeriodShares += s.PreviousPeriodShares;

                for (var i = 0; i < 12; i++)
                {
                    result.MonthlyViews[i] += s.MonthlyViews.ElementAtOrDefault(i);
                    result.MonthlyLikes[i] += s.MonthlyLikes.ElementAtOrDefault(i);
                }

                result.TopMaterials.AddRange(s.TopMaterials);
            }

            result.TopMaterials = result.TopMaterials
                .OrderByDescending(m => m.ViewCount)
                .Take(TopCount)
                .ToList();

            return result;
        }
    }
}