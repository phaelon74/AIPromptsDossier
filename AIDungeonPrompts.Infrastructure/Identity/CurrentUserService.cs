using System.Threading.Tasks;
using AIDungeonPrompts.Application.Abstractions.Identity;
using AIDungeonPrompts.Application.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AIDungeonPrompts.Infrastructure.Identity
{
	public class CurrentUserService : ICurrentUserService
	{
		private const string CurrentUserKey = "CurrentUser";
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ILogger<CurrentUserService> _logger;
		private readonly IMediator _mediator;

		public CurrentUserService(
			ILogger<CurrentUserService> logger, 
			IMediator mediator,
			IHttpContextAccessor httpContextAccessor)
		{
			_logger = logger;
			_mediator = mediator;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task SetCurrentUser(int userId)
		{
			var user = await _mediator.Send(new GetUserQuery(userId));
			if (user == null)
			{
				_logger.LogWarning($"User with ID {userId} could not be found.");
			}

			if (_httpContextAccessor.HttpContext != null)
			{
				_httpContextAccessor.HttpContext.Items[CurrentUserKey] = user;
			}
		}

		public bool TryGetCurrentUser(out GetUserViewModel? user)
		{
			user = null;
			if (_httpContextAccessor.HttpContext?.Items.TryGetValue(CurrentUserKey, out var userObj) == true)
			{
				user = userObj as GetUserViewModel;
			}
			return user != null;
		}
	}
}
