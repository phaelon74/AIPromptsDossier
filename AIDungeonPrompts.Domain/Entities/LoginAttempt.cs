using System;
using AIDungeonPrompts.Domain.Entities.Abstract;

namespace AIDungeonPrompts.Domain.Entities
{
	public class LoginAttempt : BaseDomainEntity
	{
		public int UserId { get; set; }
		public User? User { get; set; }
		public bool Success { get; set; }
		public string? IpAddress { get; set; }
		public DateTime AttemptDate { get; set; }
	}
}
