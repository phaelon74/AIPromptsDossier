using System;
using System.Threading.Tasks;
using AIDungeonPrompts.Application.Abstractions.DbContexts;
using AIDungeonPrompts.Application.Abstractions.Infrastructure;
using AIDungeonPrompts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIDungeonPrompts.Infrastructure.SystemSettings
{
	public class SystemSettingsService : ISystemSettingsService
	{
		private readonly IAIDungeonPromptsDbContext _dbContext;
		private readonly ILogger<SystemSettingsService> _logger;

		private const string MaxPageSizeKey = "MaxPageSize";
		private const string UserRegistrationEnabledKey = "UserRegistrationEnabled";
		
		private const int DefaultMaxPageSize = 100;
		private const bool DefaultUserRegistrationEnabled = true;

		public SystemSettingsService(IAIDungeonPromptsDbContext dbContext, ILogger<SystemSettingsService> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		public async Task<int> GetMaxPageSizeAsync()
		{
			var setting = await _dbContext.SystemSettings
				.FirstOrDefaultAsync(s => s.Key == MaxPageSizeKey);

			if (setting == null)
			{
				return DefaultMaxPageSize;
			}

			if (int.TryParse(setting.Value, out int value))
			{
				return value;
			}

			_logger.LogWarning($"Invalid MaxPageSize value: {setting.Value}. Using default: {DefaultMaxPageSize}");
			return DefaultMaxPageSize;
		}

		public async Task<bool> IsUserRegistrationEnabledAsync()
		{
			var setting = await _dbContext.SystemSettings
				.FirstOrDefaultAsync(s => s.Key == UserRegistrationEnabledKey);

			if (setting == null)
			{
				return DefaultUserRegistrationEnabled;
			}

			if (bool.TryParse(setting.Value, out bool value))
			{
				return value;
			}

			_logger.LogWarning($"Invalid UserRegistrationEnabled value: {setting.Value}. Using default: {DefaultUserRegistrationEnabled}");
			return DefaultUserRegistrationEnabled;
		}

		public async Task SetMaxPageSizeAsync(int maxSize)
		{
			if (maxSize < 1 || maxSize > 1000)
			{
				throw new ArgumentOutOfRangeException(nameof(maxSize), "Max page size must be between 1 and 1000");
			}

			var setting = await _dbContext.SystemSettings
				.FirstOrDefaultAsync(s => s.Key == MaxPageSizeKey);

			if (setting == null)
			{
				setting = new SystemSetting
				{
					Key = MaxPageSizeKey,
					Value = maxSize.ToString(),
					Description = "Maximum number of results per page in search queries",
					DateCreated = DateTime.UtcNow
				};
				_dbContext.SystemSettings.Add(setting);
			}
			else
			{
				setting.Value = maxSize.ToString();
				setting.DateEdited = DateTime.UtcNow;
			}

			await _dbContext.SaveChangesAsync(default);
			_logger.LogInformation($"Max page size updated to: {maxSize}");
		}

		public async Task SetUserRegistrationEnabledAsync(bool enabled)
		{
			var setting = await _dbContext.SystemSettings
				.FirstOrDefaultAsync(s => s.Key == UserRegistrationEnabledKey);

			if (setting == null)
			{
				setting = new SystemSetting
				{
					Key = UserRegistrationEnabledKey,
					Value = enabled.ToString(),
					Description = "Controls whether new user registration is allowed",
					DateCreated = DateTime.UtcNow
				};
				_dbContext.SystemSettings.Add(setting);
			}
			else
			{
				setting.Value = enabled.ToString();
				setting.DateEdited = DateTime.UtcNow;
			}

			await _dbContext.SaveChangesAsync(default);
			_logger.LogInformation($"User registration enabled: {enabled}");
		}
	}
}
