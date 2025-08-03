using Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

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
        [HttpGet("GetData/{countryCode}")]
        public async Task<IActionResult> GetWorldBankData(string countryCode)
        {
            var url = $"http://api.worldbank.org/v2/country/{countryCode}/indicator/NY.GDP.MKTP.CD?format=json";

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

    }
}
