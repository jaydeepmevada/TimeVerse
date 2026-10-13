using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels;

namespace TimeVerse.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWatchCatalogService _catalogService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IWatchCatalogService catalogService, ILogger<HomeController> logger)
        {
            _catalogService = catalogService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var homeModel = await _catalogService.GetHomeDataAsync();
                return View(homeModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage showcase data.");
                return View(new HomeViewModel());
            }
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult StatusCodeHandler(int statusCode = 404)
        {
            var accept = Request.Headers["Accept"].ToString();
            if (Request.Path.StartsWithSegments("/api") || (accept.Contains("application/json") && !accept.Contains("text/html")))
            {
                return StatusCode(statusCode, new { status = statusCode, message = statusCode == 404 ? "Resource not found." : "An unexpected anomaly occurred." });
            }

            Response.StatusCode = statusCode;
            if (statusCode == 404)
            {
                return View("NotFound");
            }

            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
