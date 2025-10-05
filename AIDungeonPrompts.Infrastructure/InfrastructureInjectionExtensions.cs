using AIDungeonPrompts.Application.Abstractions.Identity;
using AIDungeonPrompts.Application.Abstractions.Infrastructure;
using AIDungeonPrompts.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AIDungeonPrompts.Infrastructure
{
	public static class InfrastructureInjectionExtensions
	{
		public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services)
		{
			services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<IAccountLockoutService, AccountLockoutService>();
		services.AddScoped<ISystemSettingsService, SystemSettings.SystemSettingsService>();
		return services;
		}
	}
}
