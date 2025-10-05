using System.Threading.Tasks;

namespace AIDungeonPrompts.Application.Abstractions.Identity
{
	public interface IAccountLockoutService
	{
		Task RecordLoginAttemptAsync(int userId, bool success, string? ipAddress);
		Task<bool> IsAccountLockedAsync(int userId);
		Task<int?> GetLockoutDelaySecondsAsync(int userId);
		Task UnlockAccountAsync(int userId);
		Task<int> GetFailedAttemptsCountAsync(int userId);
	}
}
