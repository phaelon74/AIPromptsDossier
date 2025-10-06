using AIDungeonPrompts.Application.Abstractions.Identity;
using AIDungeonPrompts.Application.Abstractions.Infrastructure;
using AIDungeonPrompts.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AIDungeonPrompts.Infrastructure
{
	public static class InfrastructureInjectionExtensions
	{
		public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services)
		{
			services.AddHttpContextAccessor(); // Required for CurrentUserService
			services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<IAccountLockoutService, AccountLockoutService>();
		services.AddScoped<ISystemSettingsService, SystemSettings.SystemSettingsService>();
		return services;
		}
	}
}
