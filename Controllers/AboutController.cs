using Microsoft.AspNetCore.Mvc;

namespace TimeVerse.Controllers
{
    public class AboutController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
