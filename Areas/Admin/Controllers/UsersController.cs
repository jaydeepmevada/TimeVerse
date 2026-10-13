using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeVerse.Data;
using TimeVerse.Models;
using TimeVerse.Services.Interfaces;
using TimeVerse.ViewModels.Admin;

namespace TimeVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOrderService orderService,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _context = context;
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
            }

            var allUsers = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            var userItems = new List<AdminUserItemViewModel>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? RoleConstants.Customer;

                if (!string.IsNullOrWhiteSpace(role) && !string.Equals(primaryRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var ordersCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);

                userItems.Add(new AdminUserItemViewModel
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = primaryRole,
                    OrdersCount = ordersCount,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            var model = new AdminUserListViewModel
            {
                Users = userItems,
                Search = search,
                RoleFilter = role,
                TotalUsersCount = await _userManager.Users.CountAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Self-deactivation is prohibited: you cannot deactivate your own administrative account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Admin changed user {Email} active status to {IsActive}.", user.Email, user.IsActive);
                TempData["SuccessMessage"] = $"Account status for {user.Email} updated to {(user.IsActive ? "Active" : "Inactive")}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update account status.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> OrderHistory(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var customerOrders = await _orderService.GetCustomerOrdersAsync(id);

            var model = new AdminUserOrderHistoryViewModel
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = roles.FirstOrDefault() ?? RoleConstants.Customer,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Orders = customerOrders
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateSalesPerson()
        {
            return View(new CreateSalesPersonViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSalesPerson(CreateSalesPersonViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
                return View(model);
            }

            var salesUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                PhoneNumber = model.Phone.Trim(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(salesUser, model.TemporaryPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(salesUser, RoleConstants.SalesPerson);
                _logger.LogInformation("Admin created new SalesPerson {Email}.", model.Email);

                model.Succeeded = true;
                model.Message = $"Sales Representative account for {model.FirstName} {model.LastName} ({model.Email}) was created successfully.";
                ModelState.Clear();
                return View(new CreateSalesPersonViewModel
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
    }
}
