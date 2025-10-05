using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AIDungeonPrompts.Application.Helpers
{
	public static class FileValidationHelper
	{
		private static readonly string[] AllowedJsonContentTypes = 
		{
			"application/json",
			"text/json",
			"text/plain" // Some browsers send plain text for .json files
		};

		public static bool IsValidJsonFile(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return false;
			}

			// Check file size (max 5MB for JSON scenario files)
			if (file.Length > 5 * 1024 * 1024)
			{
				return false;
			}

			// Check Content-Type header
			bool validContentType = false;
			foreach (var allowedType in AllowedJsonContentTypes)
			{
				if (file.ContentType?.Contains(allowedType, StringComparison.OrdinalIgnoreCase) == true)
				{
					validContentType = true;
					break;
				}
			}

			// Also allow if filename has .json extension even if Content-Type is generic
			if (!validContentType && file.FileName?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) != true)
			{
				return false;
			}

			// Validate file signature (JSON files typically start with { or [)
			try
			{
				using var stream = file.OpenReadStream();
				var buffer = new byte[10];
				var bytesRead = stream.Read(buffer, 0, buffer.Length);
				
				if (bytesRead == 0)
				{
					return false;
				}

				// Skip UTF-8 BOM if present
				int startIndex = 0;
				if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
				{
					startIndex = 3;
				}

				// Skip whitespace
				while (startIndex < bytesRead && char.IsWhiteSpace((char)buffer[startIndex]))
				{
					startIndex++;
				}

				if (startIndex >= bytesRead)
				{
					return false;
				}

				// JSON files should start with { or [
				char firstChar = (char)buffer[startIndex];
				if (firstChar != '{' && firstChar != '[')
				{
					return false;
				}

				// Reset stream position
				stream.Position = 0;

				// Try to validate as JSON
				using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
				var content = reader.ReadToEnd();
				
				if (string.IsNullOrWhiteSpace(content))
				{
					return false;
				}

				// Attempt to parse as JSON to ensure it's valid
				try
				{
					using var jsonDoc = JsonDocument.Parse(content);
					return true;
				}
				catch (JsonException)
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
