using AIDungeonPrompts.Application.Helpers;
using FluentValidation;

namespace AIDungeonPrompts.Application.Commands.UpdatePrompt
{
	public class UpdatePromptCommandValidator : AbstractValidator<UpdatePromptCommand>
	{
		private const int MAX_SIZE = 500001;
		private const int MAX_CONTENT_LENGTH = 50000; // Maximum characters for prompt content
		private const int MAX_TITLE_LENGTH = 2000; // Maximum characters for title
		private const int MAX_DESCRIPTION_LENGTH = 5000; // Maximum characters for description
		private const int MAX_MEMORY_LENGTH = 10000; // Maximum characters for memory

		public UpdatePromptCommandValidator()
		{
			RuleFor(e => e.Id)
				.NotEmpty();
			RuleFor(e => e.PromptContent)
				.NotEmpty()
				.WithMessage("Please supply a Prompt")
				.MaximumLength(MAX_CONTENT_LENGTH)
				.WithMessage($"Prompt content must not exceed {MAX_CONTENT_LENGTH} characters");
			RuleFor(e => e.PromptTags)
				.NotEmpty()
				.WithMessage("Please supply at least a single tag")
				.When(e => !e.ParentId.HasValue);
			RuleFor(e => e.Title)
				.NotEmpty()
				.WithMessage("Please supply a Title")
				.MaximumLength(MAX_TITLE_LENGTH)
				.WithMessage($"Title must not exceed {MAX_TITLE_LENGTH} characters");
			RuleFor(e => e.Description)
				.MaximumLength(MAX_DESCRIPTION_LENGTH)
				.WithMessage($"Description must not exceed {MAX_DESCRIPTION_LENGTH} characters")
				.When(e => !string.IsNullOrEmpty(e.Description));
			RuleFor(e => e.Memory)
				.MaximumLength(MAX_MEMORY_LENGTH)
				.WithMessage($"Memory must not exceed {MAX_MEMORY_LENGTH} characters")
				.When(e => !string.IsNullOrEmpty(e.Memory));
			RuleFor(e => e.AuthorsNote)
				.MaximumLength(MAX_MEMORY_LENGTH)
				.WithMessage($"Author's Note must not exceed {MAX_MEMORY_LENGTH} characters")
				.When(e => !string.IsNullOrEmpty(e.AuthorsNote));
			RuleFor(e => e.Quests)
				.MaximumLength(MAX_MEMORY_LENGTH)
				.WithMessage($"Quests must not exceed {MAX_MEMORY_LENGTH} characters")
				.When(e => !string.IsNullOrEmpty(e.Quests));
			RuleFor(e => e.ScriptZip)
				.Must(scriptZip => scriptZip!.Length < MAX_SIZE)
				.WithMessage("File size too large (max 500kb)")
				.When(e => e.ScriptZip != null);
			RuleFor(e => e.ScriptZip)
				.Must(scriptZip => ZipHelper.IsCompressedData(scriptZip!))
				.WithMessage("Please only upload .zip files")
				.DependentRules(() =>
				{
					RuleFor(e => e.ScriptZip)
						.Must(scriptZip => ZipHelper.CheckFileContents(scriptZip!))
						.WithMessage("File was not in the expected format. Please re-export and try again.")
						.When(e => e.ScriptZip != null);
				})
				.When(e => e.ScriptZip?.Length < MAX_SIZE);
		}
	}
}
