using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebMVC.Models;
using Process.Interface;

namespace WebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeProcess _homeProcess;

        public HomeController(ILogger<HomeController> logger, IHomeProcess homeProcess)
        {
            _logger = logger;
            _homeProcess = homeProcess;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Test GetWeatherForecast()
        public async Task<ActionResult> GetWeatherForecast()
        {
            var result = await _homeProcess.GetWeatherForecast();
            return View(result);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
