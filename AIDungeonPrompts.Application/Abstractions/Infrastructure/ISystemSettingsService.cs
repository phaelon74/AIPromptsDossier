using System.Threading.Tasks;

namespace AIDungeonPrompts.Application.Abstractions.Infrastructure
{
	public interface ISystemSettingsService
	{
		Task<int> GetMaxPageSizeAsync();
		Task<bool> IsUserRegistrationEnabledAsync();
		Task SetMaxPageSizeAsync(int maxSize);
		Task SetUserRegistrationEnabledAsync(bool enabled);
	}
}
