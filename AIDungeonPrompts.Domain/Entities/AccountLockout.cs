using System;
using AIDungeonPrompts.Domain.Entities.Abstract;

namespace AIDungeonPrompts.Domain.Entities
{
	public class AccountLockout : BaseDomainEntity
	{
		public int UserId { get; set; }
		public User? User { get; set; }
		public DateTime LockoutStart { get; set; }
		public DateTime? LockoutEnd { get; set; }
		public int FailedAttempts { get; set; }
		public bool IsActive { get; set; }
		public string? LockedByAdmin { get; set; }
	}
}
