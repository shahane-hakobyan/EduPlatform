using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace EduPlatform.Services
{
    public interface IIpGeolocationService
    {
        Task<string> GetCountryCodeAsync(string? ip);
    }

    public class IpGeolocationService : IIpGeolocationService
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<IpGeolocationService> _logger;
        private const string FallbackCountryCode = "am";

        public IpGeolocationService(
            IMemoryCache cache,
            IHttpClientFactory httpFactory,
            ILogger<IpGeolocationService> logger)
        {
            _cache = cache;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<string> GetCountryCodeAsync(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1")
                return FallbackCountryCode;

            var cacheKey = $"geo:country:{ip}";

            // The sidebar flag is decoration - a lookup failure must never
            // take down the page, so everything here degrades to the fallback.
            try
            {
                if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached is not null)
                    return cached;

                var http = _httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(3);

                var result = await http.GetFromJsonAsync<GeoResponse>(
                    $"http://ip-api.com/json/{ip}?fields=countryCode");

                var code = result?.CountryCode?.ToLower() ?? FallbackCountryCode;
                _cache.Set(cacheKey, code, TimeSpan.FromDays(30));
                return code;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geolocation lookup failed for {Ip}; using fallback.", ip);
                return FallbackCountryCode;
            }
        }

        private class GeoResponse
        {
            public string? CountryCode { get; set; }
        }
    }
}