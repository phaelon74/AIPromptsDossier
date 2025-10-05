using System.Text.RegularExpressions;
using FluentValidation;

namespace AIDungeonPrompts.Application.Commands.CreateUser
{
	public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
	{
		private const int MinPasswordLength = 12;

		public CreateUserCommandValidator()
		{
			RuleFor(e => e.Username)
				.NotEmpty()
				.WithMessage("Username is required");

			RuleFor(e => e.Password)
				.NotEmpty()
				.WithMessage("Password is required")
				.MinimumLength(MinPasswordLength)
				.WithMessage($"Password must be at least {MinPasswordLength} characters long")
				.Must(HaveUpperCase)
				.WithMessage("Password must contain at least one uppercase letter")
				.Must(HaveLowerCase)
				.WithMessage("Password must contain at least one lowercase letter")
				.Must(HaveDigit)
				.WithMessage("Password must contain at least one number")
				.Must(HaveSpecialCharacter)
				.WithMessage("Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>)");
		}

		private static bool HaveUpperCase(string password)
		{
			return !string.IsNullOrEmpty(password) && Regex.IsMatch(password, @"[A-Z]");
		}

		private static bool HaveLowerCase(string password)
		{
			return !string.IsNullOrEmpty(password) && Regex.IsMatch(password, @"[a-z]");
		}

		private static bool HaveDigit(string password)
		{
			return !string.IsNullOrEmpty(password) && Regex.IsMatch(password, @"[0-9]");
		}

		private static bool HaveSpecialCharacter(string password)
		{
			return !string.IsNullOrEmpty(password) && Regex.IsMatch(password, @"[^a-zA-Z0-9]");
		}
	}
}
