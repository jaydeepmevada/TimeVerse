using Microsoft.AspNetCore.Mvc;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels;

namespace TimeVerse.Controllers
{
    public class ContactController : Controller
    {
        private readonly IWatchCatalogService _catalogService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IWatchCatalogService catalogService, ILogger<ContactController> logger)
        {
            _catalogService = catalogService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _catalogService.SubmitInquiryAsync(model);
                if (success)
                {
                    model.IsSubmittedSuccess = true;
                    model.FeedbackMessage = "Thank you for reaching out to TimeVerse. Our sales concierge will review your inquiry and connect with you shortly.";
                    ModelState.Clear();
                    return View(new ContactViewModel
                    {
                        IsSubmittedSuccess = true,
                        FeedbackMessage = "Thank you for reaching out to TimeVerse. Our sales concierge will review your inquiry and connect with you shortly."
                    });
                }

                ModelState.AddModelError(string.Empty, "An error occurred while submitting your message. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact inquiry.");
                ModelState.AddModelError(string.Empty, "A server error occurred. Please try again later.");
                return View(model);
            }
        }
    }
}
