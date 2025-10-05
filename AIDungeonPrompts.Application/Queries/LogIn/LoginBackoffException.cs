using System;

namespace AIDungeonPrompts.Application.Queries.LogIn
{
	public class LoginBackoffException : Exception
	{
		public int DelaySeconds { get; }

		public LoginBackoffException(int delaySeconds) 
			: base($"Too many failed login attempts. Please wait {delaySeconds} seconds before trying again.")
		{
			DelaySeconds = delaySeconds;
		}
	}
}
