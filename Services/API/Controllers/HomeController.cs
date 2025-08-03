using Entities;
using Entities.DigitalPayment;
using Entities.Tariff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Wrap;
using System.ComponentModel;
using System.Xml.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/Home")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClientFactory = httpClientFactory;
        }

        //private static readonly string[] Summaries = new[]
        //{
        //    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        //};

        //[HttpGet("GetWeatherForecast")]
        //public IEnumerable<WeatherForecast> Get()
        //{
        //    return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //    {
        //        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        //        TemperatureC = Random.Shared.Next(-20, 55),
        //        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //    })
        //    .ToArray();
        //}

        //// Test
        //[HttpGet("GetData/{CountryCode}")]
        //public async Task<IActionResult> GetWorldBankData(string CountryCode)
        //{
        //    var url = $"http://api.worldbank.org/v2/country/{CountryCode}/indicator/NY.GDP.MKTP.CD?format=json";

        //    try
        //    {
        //        var response = await _httpClient.GetAsync(url);
        //        response.EnsureSuccessStatusCode();

        //        var content = await response.Content.ReadAsStringAsync();
        //        return Content(content, "application/json");
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        _logger.LogError(ex, "Error fetching World Bank data.");
        //        return StatusCode(503, "Failed to contact World Bank API.");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _logger.LogError(ex, "Unhandled error.");
        //        return StatusCode(500, "Internal server error.");
        //    }
        //}

        // 1st API endpoint: multiple basket of world bank API endpoints, aggregated into one.
        // Tariff rate, applied weighted mean, all products: TM.TAX.MRCH.WM.AR.ZS
        // Trade % of GDP: NE.TRD.GNFS.ZS
        // FDI inflows: BX.KLT.DINV.WD.GD.ZS
        // Manufacturing exports: TX.VAL.MANF.ZS.UN

        [HttpGet("GetTarrifEscalationVsTradeDependenceData")]
        public async Task<IActionResult> GetTarrifEscalationVsTradeDependenceData([FromQuery] string[] CountryCodes, [FromQuery] string timePeriodFrom, [FromQuery]string timePeriodTo, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WorldBankClient");

                // multiple call to a single country, repeat foreach country
                var tasks = CountryCodes
                .SelectMany(country => _tariffIndicators.Select(async indicator =>
                {
                    var url = $"https://api.worldbank.org/v2/countries/{country}/indicators/{indicator}?date={timePeriodFrom}:{timePeriodTo}&format=json";
                    var response = await client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var data = ExtractTarrifRelatedData(content);

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

        // 2nc API endpoint: Made or received a digital payment. The only one with different json format.
        [HttpGet("GetMadeOrReceivedADigitalPaymentData")]
        public async Task<IActionResult> GetMadeOrReceivedADigitalPaymentData([FromQuery] string[] CountryCodes, [FromQuery] string timePeriodFrom, [FromQuery] string timePeriodTo, CancellationToken cancellationToken)
        {

            try
            {
                var client = _httpClientFactory.CreateClient("WorldBankClient");

                // single call, multi-country
                var url = $"https://data360api.worldbank.org/data360/data?DATABASE_ID=WB_FINDEX&INDICATOR=WB_FINDEX_G20_ANY&REF_AREA={string.Join(",", CountryCodes)}&timePeriodFrom={timePeriodFrom}&timePeriodTo={timePeriodTo}&skip=0";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var container = ExtractDigitalPaymentRelatedData(content);

                var records = container?.value ?? new List<FlattenMakeOrReceivedDigitalPaymentRecord>();

                return Ok(records);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching World Bank data.");
                return StatusCode(503, "Failed to contact World Bank API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // 3rd API endpoint: Electric power consumption in kilowatt-hours per capita
        // https://api.worldbank.org/v2/country/MY/indicator/EG.USE.ELEC.KH.PC?date=2015:2025&format=json
        [HttpGet("GetElectricPowerConsumptionInKwHrsPerCapitaData")]
        public async Task<IActionResult> GetElectricPowerConsumptionInKwHrsPerCapitaData([FromQuery] string[] CountryCodes, [FromQuery] string timePeriodFrom, [FromQuery] string timePeriodTo, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WorldBankClient");

                var countryList = string.Join(";", CountryCodes);
                var url = $"https://api.worldbank.org/v2/country/{countryList}/indicator/EG.USE.ELEC.KH.PC?date={timePeriodFrom}:{timePeriodTo}&format=json";

                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = ExtractTarrifRelatedData(content);

                var flatList = data.Select(x => new FlatIndicatorRecord
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
                }).ToList();

                return Ok(flatList);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching World Bank data.");
                return StatusCode(503, "Failed to contact World Bank API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // 4th API endpoint: Fertility rate, total (births per woman)
        // https://api.worldbank.org/v2/country/MY/indicator/SP.DYN.TFRT.IN?date=2000:2025&format=json
        [HttpGet("GetFertilityRateData")]
        public async Task<IActionResult> GetFertilityRateData([FromQuery] string[] CountryCodes, [FromQuery] string timePeriodFrom, [FromQuery] string timePeriodTo, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WorldBankClient");

                var countryList = string.Join(";", CountryCodes);
                var url = $"https://api.worldbank.org/v2/country/{countryList}/indicator/SP.DYN.TFRT.IN?date={timePeriodFrom}:{timePeriodTo}&format=json";

                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = ExtractTarrifRelatedData(content);

                var flatList = data.Select(x => new FlatIndicatorRecord
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
                }).ToList();

                return Ok(flatList);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching World Bank data.");
                return StatusCode(503, "Failed to contact World Bank API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // 5th API endpoint: Gini index
        [HttpGet("GetGiniIndex")]
        // https://api.worldbank.org/v2/country/MY/indicator/SI.POV.GINI?date=2000:2025&format=json
        public async Task<IActionResult> GetGiniIndex([FromQuery] string[] CountryCodes, [FromQuery] string timePeriodFrom, [FromQuery] string timePeriodTo, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WorldBankClient");

                var countryList = string.Join(";", CountryCodes);
                var url = $"https://api.worldbank.org/v2/country/{countryList}/indicator/SI.POV.GINI?date={timePeriodFrom}:{timePeriodTo}&format=json";

                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = ExtractTarrifRelatedData(content);

                var flatList = data.Select(x => new FlatIndicatorRecord
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
                }).ToList();

                return Ok(flatList);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching World Bank data.");
                return StatusCode(503, "Failed to contact World Bank API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Helper, to be moved to Helper class.
        private List<ViewModelRawTariffData> ExtractTarrifRelatedData(string json)
        {
            var wrapper = JsonConvert.DeserializeObject<List<object>>(json);
            var rawDataJson = wrapper[1].ToString();
            return JsonConvert.DeserializeObject<List<ViewModelRawTariffData>>(rawDataJson);
        }

        // Temporary helper for tariff related api endpoints
        private static readonly List<string> _tariffIndicators = new()
        {
            "TX.VAL.MANF.ZS.UN",
            "BX.KLT.DINV.WD.GD.ZS",
            "TM.TAX.MRCH.WM.AR.ZS",
            "NE.TRD.GNFS.ZS"
        };

        private ViewModelRawMakeDigitalPaymentData ExtractDigitalPaymentRelatedData(string json)
        {
            return JsonConvert.DeserializeObject<ViewModelRawMakeDigitalPaymentData>(json);
        }
    }

}
