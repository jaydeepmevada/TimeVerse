using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.ViewModels.Account;
using TimeVerse.ViewModels.Customer;

namespace TimeVerse.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleConstants.Customer)]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            model.Email = user.Email ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                model.Succeeded = true;
                model.Message = "Your collector profile details have been updated successfully.";
                return View(model);
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Customer {Email} successfully changed password.", user.Email);
                model.Succeeded = true;
                model.Message = "Your security password has been changed successfully.";
                ModelState.Clear();
                return View(new ChangePasswordViewModel
                {
                    Succeeded = true,
                    Message = model.Message
                });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var list = new List<AddressViewModel>();
            try
            {
                list = await _dbContext.Addresses
                    .Where(a => a.ApplicationUserId == user.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .Select(a => new AddressViewModel
                    {
                        Id = a.Id,
                        FullName = a.FullName,
                        AddressLine1 = a.AddressLine1,
                        AddressLine2 = a.AddressLine2,
                        City = a.City,
                        State = a.State,
                        Country = a.Country,
                        PostalCode = a.PostalCode,
                        Phone = a.Phone,
                        IsDefault = a.IsDefault
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving addresses for user {UserId}", user.Id);
            }

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(AddressViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Addresses));
            }

            try
            {
                if (model.IsDefault)
                {
                    var existingDefaults = await _dbContext.Addresses
                        .Where(a => a.ApplicationUserId == user.Id && a.IsDefault)
                        .ToListAsync();
                    foreach (var addr in existingDefaults)
                    {
                        addr.IsDefault = false;
                    }
                }

                var entity = new Address
                {
                    ApplicationUserId = user.Id,
                    FullName = model.FullName.Trim(),
                    AddressLine1 = model.AddressLine1.Trim(),
                    AddressLine2 = model.AddressLine2?.Trim(),
                    City = model.City.Trim(),
                    State = model.State.Trim(),
                    Country = model.Country.Trim(),
                    PostalCode = model.PostalCode.Trim(),
                    Phone = model.Phone.Trim(),
                    IsDefault = model.IsDefault,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Addresses.Add(entity);
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "New shipping address saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customer address.");
                TempData["ErrorMessage"] = "Could not save address at this time.";
            }

            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            try
            {
                var address = await _dbContext.Addresses
                    .FirstOrDefaultAsync(a => a.Id == id && a.ApplicationUserId == user.Id);

                if (address != null)
                {
                    _dbContext.Addresses.Remove(address);
                    await _dbContext.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Shipping address removed.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer address.");
            }

            return RedirectToAction(nameof(Addresses));
        }
    }
}
