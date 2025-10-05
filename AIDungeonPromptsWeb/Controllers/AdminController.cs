using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIDungeonPrompts.Application.Abstractions.DbContexts;
using AIDungeonPrompts.Application.Abstractions.Identity;
using AIDungeonPrompts.Domain.Entities;
using AIDungeonPrompts.Domain.Enums;
using AIDungeonPrompts.Web.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIDungeonPrompts.Web.Controllers
{
	[Authorize(Policy = PolicyValueConstants.AdminsOnly)]
	public class AdminController : Controller
	{
		private readonly IAIDungeonPromptsDbContext _dbContext;
		private readonly IAccountLockoutService _lockoutService;

		public AdminController(IAIDungeonPromptsDbContext dbContext, IAccountLockoutService lockoutService)
		{
			_dbContext = dbContext;
			_lockoutService = lockoutService;
		}

		[HttpGet("[controller]")]
		public async Task<IActionResult> Index()
		{
			return View();
		}

		[HttpGet("[controller]/users")]
		public async Task<IActionResult> Users(CancellationToken cancellationToken)
		{
			var users = await _dbContext.Users
				.Include(u => u.AccountLockouts.Where(l => l.IsActive))
				.OrderBy(u => u.Username)
				.ToListAsync(cancellationToken);

			return View(users);
		}

		[HttpPost("[controller]/unlock/{userId}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UnlockAccount(int userId)
		{
			await _lockoutService.UnlockAccountAsync(userId);
			TempData["SuccessMessage"] = "Account has been unlocked successfully.";
			return RedirectToAction("Users");
		}

		[HttpPost("[controller]/update-role/{userId}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateRole(int userId, RoleEnum role, CancellationToken cancellationToken)
		{
			var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
			if (user == null)
			{
				return NotFound();
			}

			user.Role = role;
			user.DateEdited = System.DateTime.UtcNow;
			_dbContext.Users.Update(user);
			await _dbContext.SaveChangesAsync(cancellationToken);

			TempData["SuccessMessage"] = $"User role updated successfully.";
			return RedirectToAction("Users");
		}

		[HttpPost("[controller]/reset-password/{userId}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(int userId, string newPassword, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 12)
			{
				TempData["ErrorMessage"] = "Password must be at least 12 characters long.";
				return RedirectToAction("Users");
			}

			var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
			if (user == null)
			{
				return NotFound();
			}

			user.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword);
			user.DateEdited = System.DateTime.UtcNow;
			_dbContext.Users.Update(user);
			await _dbContext.SaveChangesAsync(cancellationToken);

			TempData["SuccessMessage"] = "Password reset successfully.";
			return RedirectToAction("Users");
		}
	}
}
