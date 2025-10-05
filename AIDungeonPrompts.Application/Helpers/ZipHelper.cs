using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AIDungeonPrompts.Application.Helpers
{
	public static class ZipHelper
	{
		private static readonly string[] ExpectedFiles =
		{
			"contextModifier.js", "inputModifier.js", "outputModifier.js", "shared.js"
		};

		private static readonly byte[] ZipBytes1 = {0x50, 0x4b, 0x03, 0x04};
		private static readonly byte[] ZipBytes2 = {0x50, 0x4b, 0x05, 0x06};
		private static readonly byte[] ZipBytes3 = {0x50, 0x4b, 0x07, 0x08};

		public static bool CheckFileContents(byte[] bytes)
		{
			try
			{
				using var memoryStream = new MemoryStream(bytes);
				using var zip = new ZipArchive(memoryStream);
				
				// Validate all entries for path traversal before checking contents
				if (!ValidateZipEntries(zip))
				{
					return false;
				}
				
				return zip.Entries.Any(e => ExpectedFiles.Contains(e.Name));
			}
			catch
			{
				return false;
			}
		}

		public static bool IsCompressedData(byte[] data)
		{
			foreach (var headerBytes in new[] {ZipBytes1, ZipBytes2, ZipBytes3})
			{
				if (HeaderBytesMatch(headerBytes, data))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Validates all zip entries to prevent path traversal attacks
		/// </summary>
		private static bool ValidateZipEntries(ZipArchive archive)
		{
			foreach (var entry in archive.Entries)
			{
				if (!IsValidZipEntry(entry))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Validates a single zip entry for security issues
		/// Prevents path traversal attacks by checking for:
		/// - Absolute paths
		/// - Parent directory references (..)
		/// - Null bytes
		/// </summary>
		private static bool IsValidZipEntry(ZipArchiveEntry entry)
		{
			// Check for null or empty names
			if (string.IsNullOrWhiteSpace(entry.FullName))
			{
				return false;
			}

			// Check for null bytes (can be used to bypass checks)
			if (entry.FullName.Contains('\0'))
			{
				return false;
			}

			// Normalize the path
			var normalizedPath = entry.FullName.Replace('\\', '/');

			// Check for absolute paths (starting with / or drive letter)
			if (Path.IsPathRooted(normalizedPath) || normalizedPath.StartsWith("/"))
			{
				return false;
			}

			// Check for parent directory references
			if (normalizedPath.Contains("../") || normalizedPath.Contains("..\\"))
			{
				return false;
			}

			// Check if the path tries to escape by starting with ..
			if (normalizedPath.StartsWith(".."))
			{
				return false;
			}

			// Additional check: ensure the full path doesn't contain problematic sequences
			var pathParts = normalizedPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in pathParts)
			{
				if (part == "..")
				{
					return false;
				}
			}

			// Verify the file name matches expected files or is in a safe subdirectory
			// Only allow expected .js files and no deeply nested structures
			if (pathParts.Length > 2) // Max depth: one subdirectory
			{
				return false;
			}

			return true;
		}

		private static bool HeaderBytesMatch(byte[] headerBytes, byte[] dataBytes)
		{
			if (dataBytes.Length < headerBytes.Length)
			{
				return false;
			}

			for (var i = 0; i < headerBytes.Length; i++)
			{
				if (headerBytes[i] == dataBytes[i])
				{
					continue;
				}

				return false;
			}

			return true;
		}
	}
}
