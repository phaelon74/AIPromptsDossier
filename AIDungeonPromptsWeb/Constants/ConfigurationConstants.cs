namespace AIDungeonPrompts.Web.Constants
{
	/// <summary>
	/// Configuration-related constants for the application.
	/// </summary>
	public static class ConfigurationConstants
	{
		/// <summary>
		/// The name of the primary database connection string in configuration.
		/// </summary>
		public const string DatabaseConnectionName = "AIDungeonPrompt";

		/// <summary>
		/// Path to Docker secret for main database password.
		/// </summary>
		public const string DatabasePasswordSecretPath = "/run/secrets/db_password";

		/// <summary>
		/// Path to Docker secret for Serilog database password.
		/// </summary>
		public const string SerilogPasswordSecretPath = "/run/secrets/serilog_db_password";

		/// <summary>
		/// Maximum request body size in bytes (10 MB).
		/// </summary>
		public const int MaxRequestBodySizeBytes = 10 * 1024 * 1024;

		/// <summary>
		/// Maximum file upload size in bytes (10 MB).
		/// </summary>
		public const int MaxFileUploadSizeBytes = 10 * 1024 * 1024;
	}
}
