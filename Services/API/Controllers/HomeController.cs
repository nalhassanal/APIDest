using Entities;
using Entities.Tariff;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/Home")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
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
        public async Task<IActionResult> GetTarrifEscalationVsTradeDependenceData(string CountryCode, string StartYear, string EndYear)
        {
            try
            {
                using var httpClient = new HttpClient();

                // Call World Bank APIs
                var manufacturesExportsTask = httpClient.GetStringAsync($"https://api.worldbank.org/v2/countries/{CountryCode}/indicators/TX.VAL.MANF.ZS.UN?date={StartYear}:{EndYear}&format=json");
                var fdiNetInflowsTask = httpClient.GetStringAsync($"https://api.worldbank.org/v2/countries/{CountryCode}/indicators/BX.KLT.DINV.WD.GD.ZS?date={StartYear}:{EndYear}&format=json");
                var tariffRateTask = httpClient.GetStringAsync($"https://api.worldbank.org/v2/countries/{CountryCode}/indicators/TM.TAX.MRCH.WM.AR.ZS?date={StartYear}:{EndYear}&format=json");
                var tradePercentageTask = httpClient.GetStringAsync($"https://api.worldbank.org/v2/countries/{CountryCode}/indicators/NE.TRD.GNFS.ZS?date={StartYear}:{EndYear}&format=json");

                await Task.WhenAll(manufacturesExportsTask, fdiNetInflowsTask, tariffRateTask, tradePercentageTask);

                // Deserialize
                var manufacturesRaw = ExtractData(manufacturesExportsTask.Result);
                var fdiRaw = ExtractData(fdiNetInflowsTask.Result);
                var tariffRaw = ExtractData(tariffRateTask.Result);
                var tradeRaw = ExtractData(tradePercentageTask.Result);

                var manufactures = manufacturesRaw.Select(x => new ViewModelManufacturesExports
                {
                    IndicatorId = x.Indicator?.Id,
                    IndicatorName = x.Indicator?.Value,
                    CountryId = x.Country?.Id,
                    CountryName = x.Country?.Value,
                    CountryIso3Code = x.CountryIso3Code,
                    Year = int.TryParse(x.Date, out var y) ? y : 0,
                    Value = x.Value,
                    Unit = x.Unit,
                    ObsStatus = x.ObsStatus,
                    Decimal = x.Decimal
                }).ToList();

                var fdi = fdiRaw.Select(x => new ViewModelForeignDirectInvestmentNetInflows
                {
                    IndicatorId = x.Indicator?.Id,
                    IndicatorName = x.Indicator?.Value,
                    CountryId = x.Country?.Id,
                    CountryName = x.Country?.Value,
                    CountryIso3Code = x.CountryIso3Code,
                    Year = int.TryParse(x.Date, out var y) ? y : 0,
                    Value = x.Value,
                    Unit = x.Unit,
                    ObsStatus = x.ObsStatus,
                    Decimal = x.Decimal
                }).ToList();

                var tariff = tariffRaw.Select(x => new ViewModelTariffRateAppliedWeightedMeanAllProducts
                {
                    IndicatorId = x.Indicator?.Id,
                    IndicatorName = x.Indicator?.Value,
                    CountryId = x.Country?.Id,
                    CountryName = x.Country?.Value,
                    CountryIso3Code = x.CountryIso3Code,
                    Year = int.TryParse(x.Date, out var y) ? y : 0,
                    Value = x.Value,
                    Unit = x.Unit,
                    ObsStatus = x.ObsStatus,
                    Decimal = x.Decimal
                }).ToList();

                var trade = tradeRaw.Select(x => new ViewModelTradePercentageOfGDP
                {
                    IndicatorId = x.Indicator?.Id,
                    IndicatorName = x.Indicator?.Value,
                    CountryId = x.Country?.Id,
                    CountryName = x.Country?.Value,
                    CountryIso3Code = x.CountryIso3Code,
                    Year = int.TryParse(x.Date, out var y) ? y : 0,
                    Value = x.Value,
                    Unit = x.Unit,
                    ObsStatus = x.ObsStatus,
                    Decimal = x.Decimal
                }).ToList();

                var result = new ViewModelTariffEscalationVsTradeDependence
                {
                    Country = CountryCode,
                    ManufacturesExports = manufactures,
                    ForeignDirectInvestmentNetInflows = fdi,
                    TariffRateAppliedWeightedMeanAllProducts = tariff,
                    TradePercentageOfGDP = trade
                };

                return Ok(result);
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

        private List<RawWorldBankData> ExtractData(string json)
        {
            var wrapper = JsonConvert.DeserializeObject<List<object>>(json);
            var rawDataJson = wrapper[1].ToString();
            return JsonConvert.DeserializeObject<List<RawWorldBankData>>(rawDataJson);
        }
    }

}
