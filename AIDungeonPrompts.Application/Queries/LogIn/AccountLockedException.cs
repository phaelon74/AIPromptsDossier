using System;

namespace AIDungeonPrompts.Application.Queries.LogIn
{
	public class AccountLockedException : Exception
	{
		public AccountLockedException() : base("This account has been locked. Please contact an administrator.")
		{
		}
	}
}
