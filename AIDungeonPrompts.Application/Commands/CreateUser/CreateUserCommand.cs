using System;
using System.Threading;
using System.Threading.Tasks;
using AIDungeonPrompts.Application.Abstractions.DbContexts;
using AIDungeonPrompts.Application.Abstractions.Infrastructure;
using AIDungeonPrompts.Application.Exceptions;
using AIDungeonPrompts.Domain.Entities;
using AIDungeonPrompts.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AIDungeonPrompts.Application.Commands.CreateUser
{
	public class CreateUserCommand : IRequest<int>
	{
		public string Password { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
	}

	public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
	{
		private readonly IAIDungeonPromptsDbContext _dbContext;
		private readonly ISystemSettingsService _systemSettingsService;

		public CreateUserCommandHandler(IAIDungeonPromptsDbContext dbContext, ISystemSettingsService systemSettingsService)
		{
			_dbContext = dbContext;
			_systemSettingsService = systemSettingsService;
		}

		public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
		{
			if (await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == request.Username, cancellationToken) != null)
			{
				throw new UsernameNotUniqueException();
			}

			// Check if this is the first user in the system
			bool isFirstUser = !await _dbContext.Users.AnyAsync(cancellationToken);

			var user = new User
			{
				DateCreated = DateTime.UtcNow,
				Password = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
				Username = request.Username,
				Role = isFirstUser ? RoleEnum.Admin : RoleEnum.None // First user is automatically admin
			};

			_dbContext.Users.Add(user);
			await _dbContext.SaveChangesAsync(cancellationToken);

			// Auto-disable registration after first user is created
			if (isFirstUser)
			{
				await _systemSettingsService.SetUserRegistrationEnabledAsync(false);
			}

			return user.Id;
		}
	}
}
