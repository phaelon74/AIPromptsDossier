using System.Threading;
using System.Threading.Tasks;
using AIDungeonPrompts.Application.Abstractions.DbContexts;
using AIDungeonPrompts.Application.Abstractions.Identity;
using AIDungeonPrompts.Application.Helpers;
using AIDungeonPrompts.Application.Queries.GetUser;
using AIDungeonPrompts.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AIDungeonPrompts.Application.Queries.LogIn
{
	public class LogInQuery : IRequest<GetUserViewModel>
	{
		public LogInQuery(string username, string password, string? ipAddress = null)
		{
			Username = username;
			Password = password;
			IpAddress = ipAddress;
		}

		public string Password { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string? IpAddress { get; set; }
	}

	public class LogInQueryHandler : IRequestHandler<LogInQuery, GetUserViewModel>
	{
		private readonly IAIDungeonPromptsDbContext _dbContext;
		private readonly IAccountLockoutService _lockoutService;

		public LogInQueryHandler(IAIDungeonPromptsDbContext dbContext, IAccountLockoutService lockoutService)
		{
			_dbContext = dbContext;
			_lockoutService = lockoutService;
		}

		public async Task<GetUserViewModel> Handle(LogInQuery request, CancellationToken cancellationToken = default)
		{
			var username = NpgsqlHelper.SafeIlike(request.Username);
			User? user = await _dbContext
				.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(e => EF.Functions.ILike(e.Username, username, NpgsqlHelper.EscapeChar), cancellationToken);

			// Check if account exists first to record failed attempts
			if (user == null)
			{
				// Don't record attempt if user doesn't exist (prevents enumeration)
				throw new LoginFailedException();
			}

			// Check if account is locked
			if (await _lockoutService.IsAccountLockedAsync(user.Id))
			{
				throw new AccountLockedException();
			}

			// Check if there's a backoff delay
			var delaySeconds = await _lockoutService.GetLockoutDelaySecondsAsync(user.Id);
			if (delaySeconds.HasValue && delaySeconds.Value > 0)
			{
				throw new LoginBackoffException(delaySeconds.Value);
			}

			// Verify password
			bool passwordValid = user.Password != null && BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.Password);

			// Record the login attempt
			await _lockoutService.RecordLoginAttemptAsync(user.Id, passwordValid, request.IpAddress);

			if (!passwordValid)
			{
				throw new LoginFailedException();
			}

			return new GetUserViewModel {Id = user.Id, Username = user.Username, Role = user.Role};
		}
	}
}
