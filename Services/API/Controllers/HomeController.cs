using Entities;
using Entities.Tariff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Wrap;

namespace API.Controllers
{
    [ApiController]
    [Route("api/Home")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet("GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        // Test
        [HttpGet("GetData/{CountryCode}")]
        public async Task<IActionResult> GetWorldBankData(string CountryCode)
        {
            var url = $"http://api.worldbank.org/v2/country/{CountryCode}/indicator/NY.GDP.MKTP.CD?format=json";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching World Bank data.");
                return StatusCode(503, "Failed to contact World Bank API.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unhandled error.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Test: Get a basket of data from world bank
        [HttpGet("GetTarrifEscalationVsTradeDependenceData")]
        [ProducesResponseType(typeof(List<FlatIndicatorRecord>), 200)]
        public async Task<IActionResult> GetTarrifEscalationVsTradeDependenceData([FromQuery] string[] CountryCodes, [FromQuery] string StartYear, [FromQuery]string EndYear, CancellationToken cancellationToken)
        {
            // If query data exists in cache, return cached result
            var cacheKey = $"WB_{string.Join('_', CountryCodes)}_{StartYear}_{EndYear}";

            if (_cache.TryGetValue(cacheKey, out List<FlatIndicatorRecord> cachedResult))
            {
                return Ok(cachedResult);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("WorldBankClient");

                var tasks = CountryCodes
                .SelectMany(country => _indicators.Select(async indicator =>
                {
                    var url = $"https://api.worldbank.org/v2/countries/{country}/indicators/{indicator}?date={StartYear}:{EndYear}&format=json";
                    var response = await client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var data = ExtractData(content);

                    return data.Select(x => new FlatIndicatorRecord
                    {
                        CountryId = x.Country?.Id,
                        CountryName = x.Country?.Value,
                        CountryIso3Code = x.CountryIso3Code,
                        Year = int.TryParse(x.Date, out var y) ? y : 0,
                        IndicatorId = x.Indicator?.Id,
                        IndicatorName = x.Indicator?.Value,
                        Value = x.Value,
                        Unit = x.Unit,
                        ObsStatus = x.ObsStatus,
                        Decimal = x.Decimal
                    });
                }))
                .ToList();

                var results = await Task.WhenAll(tasks);
                var flatList = results.SelectMany(r => r).ToList();
                
                // Store cache for 12 hours
                _cache.Set(cacheKey, flatList, TimeSpan.FromHours(12));

                return Ok(flatList);

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching World Bank data.");
                return StatusCode(503, "Failed to contact World Bank API.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unhandled error.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Helper, to be moved to Helper class.
        private List<RawWorldBankData> ExtractData(string json)
        {
            var wrapper = JsonConvert.DeserializeObject<List<object>>(json);
            var rawDataJson = wrapper[1].ToString();
            return JsonConvert.DeserializeObject<List<RawWorldBankData>>(rawDataJson);
        }

        // Temporary helper
        private static readonly List<string> _indicators = new()
        {
            "TX.VAL.MANF.ZS.UN",
            "BX.KLT.DINV.WD.GD.ZS",
            "TM.TAX.MRCH.WM.AR.ZS",
            "NE.TRD.GNFS.ZS"
        };

    }

}
