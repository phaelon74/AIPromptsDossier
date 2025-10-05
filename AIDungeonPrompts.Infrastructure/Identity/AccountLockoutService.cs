using System;
using System.Linq;
using System.Threading.Tasks;
using AIDungeonPrompts.Application.Abstractions.DbContexts;
using AIDungeonPrompts.Application.Abstractions.Identity;
using AIDungeonPrompts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIDungeonPrompts.Infrastructure.Identity
{
	public class AccountLockoutService : IAccountLockoutService
	{
		private readonly IAIDungeonPromptsDbContext _dbContext;
		private readonly ILogger<AccountLockoutService> _logger;
		
		private const int MaxFailedAttempts = 10;
		private const int MinutesBeforeBackoffStarts = 5;
		private const int BackoffStartAttempt = 5;

		public AccountLockoutService(IAIDungeonPromptsDbContext dbContext, ILogger<AccountLockoutService> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		public async Task RecordLoginAttemptAsync(int userId, bool success, string? ipAddress)
		{
			var attempt = new LoginAttempt
			{
				UserId = userId,
				Success = success,
				IpAddress = ipAddress,
				AttemptDate = DateTime.UtcNow,
				DateCreated = DateTime.UtcNow
			};

			_dbContext.LoginAttempts.Add(attempt);

			if (!success)
			{
				var failedAttempts = await GetFailedAttemptsCountAsync(userId);

				if (failedAttempts >= MaxFailedAttempts)
				{
					// Lock the account permanently until admin unlocks
					var lockout = new AccountLockout
					{
						UserId = userId,
						LockoutStart = DateTime.UtcNow,
						LockoutEnd = null, // Null means permanent lock
						FailedAttempts = failedAttempts,
						IsActive = true,
						DateCreated = DateTime.UtcNow
					};

					_dbContext.AccountLockouts.Add(lockout);
					_logger.LogWarning($"User {userId} has been locked out after {failedAttempts} failed login attempts");
				}
			}
			else
			{
				// Reset failed attempts on successful login
				await ResetFailedAttemptsAsync(userId);
			}

			await _dbContext.SaveChangesAsync(default);
		}

		public async Task<bool> IsAccountLockedAsync(int userId)
		{
			var activeLockout = await _dbContext.AccountLockouts
				.Where(l => l.UserId == userId && l.IsActive)
				.OrderByDescending(l => l.LockoutStart)
				.FirstOrDefaultAsync();

			if (activeLockout == null)
			{
				return false;
			}

			// Check if lockout has expired
			if (activeLockout.LockoutEnd.HasValue && activeLockout.LockoutEnd.Value < DateTime.UtcNow)
			{
				activeLockout.IsActive = false;
				_dbContext.AccountLockouts.Update(activeLockout);
				await _dbContext.SaveChangesAsync(default);
				return false;
			}

			// If LockoutEnd is null, it's a permanent lock (requires admin intervention)
			return true;
		}

		public async Task<int?> GetLockoutDelaySecondsAsync(int userId)
		{
			var failedAttempts = await GetFailedAttemptsCountAsync(userId);

			// No delay for first 5 attempts
			if (failedAttempts < BackoffStartAttempt)
			{
				return null;
			}

			// 1 minute backoff for attempts 6-10
			if (failedAttempts >= BackoffStartAttempt && failedAttempts < MaxFailedAttempts)
			{
				// Check if the user needs to wait
				var lastAttempt = await _dbContext.LoginAttempts
					.Where(l => l.UserId == userId && !l.Success)
					.OrderByDescending(l => l.AttemptDate)
					.FirstOrDefaultAsync();

				if (lastAttempt != null)
				{
					var timeSinceLastAttempt = DateTime.UtcNow - lastAttempt.AttemptDate;
					var requiredDelay = TimeSpan.FromMinutes(1);

					if (timeSinceLastAttempt < requiredDelay)
					{
						return (int)(requiredDelay - timeSinceLastAttempt).TotalSeconds;
					}
				}
			}

			return null;
		}

		public async Task UnlockAccountAsync(int userId)
		{
			var activeLockouts = await _dbContext.AccountLockouts
				.Where(l => l.UserId == userId && l.IsActive)
				.ToListAsync();

			foreach (var lockout in activeLockouts)
			{
				lockout.IsActive = false;
				lockout.LockoutEnd = DateTime.UtcNow;
				_dbContext.AccountLockouts.Update(lockout);
			}

			await ResetFailedAttemptsAsync(userId);
			await _dbContext.SaveChangesAsync(default);
			
			_logger.LogInformation($"Account {userId} has been unlocked");
		}

		public async Task<int> GetFailedAttemptsCountAsync(int userId)
		{
			// Count failed attempts in the last time window (e.g., 15 minutes)
			var cutoffTime = DateTime.UtcNow.AddMinutes(-15);
			
			return await _dbContext.LoginAttempts
				.Where(l => l.UserId == userId && !l.Success && l.AttemptDate > cutoffTime)
				.CountAsync();
		}

		private async Task ResetFailedAttemptsAsync(int userId)
		{
			// Deactivate any temporary lockouts
			var tempLockouts = await _dbContext.AccountLockouts
				.Where(l => l.UserId == userId && l.IsActive && l.LockoutEnd.HasValue)
				.ToListAsync();

			foreach (var lockout in tempLockouts)
			{
				lockout.IsActive = false;
				_dbContext.AccountLockouts.Update(lockout);
			}
		}
	}
}
