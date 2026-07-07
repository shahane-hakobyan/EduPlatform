using System.Net.Http.Json;
using StackExchange.Redis;

namespace EduPlatform.Services
{
    public interface IIpGeolocationService
    {
        Task<string> GetCountryCodeAsync(string? ip);
    }

    public class IpGeolocationService : IIpGeolocationService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHttpClientFactory _httpFactory;
        private const string FallbackCountryCode = "am";

        public IpGeolocationService(IConnectionMultiplexer redis, IHttpClientFactory httpFactory)
        {
            _redis = redis;
            _httpFactory = httpFactory;
        }

        public async Task<string> GetCountryCodeAsync(string? ip)
        {      
            if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1")
                return FallbackCountryCode;

            var db = _redis.GetDatabase();
            var cacheKey = $"geo:country:{ip}";

            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue) return cached.ToString();

            try
            {
                var http = _httpFactory.CreateClient();
                var result = await http.GetFromJsonAsync<GeoResponse>(
                    $"http://ip-api.com/json/{ip}?fields=countryCode");

                var code = result?.CountryCode?.ToLower() ?? FallbackCountryCode;
                await db.StringSetAsync(cacheKey, code, TimeSpan.FromDays(30));
                return code;
            }
            catch
            {
                return FallbackCountryCode; 
            }
        }

        private class GeoResponse
        {
            public string? CountryCode { get; set; }
        }
    }
}