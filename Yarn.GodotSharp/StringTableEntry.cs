using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotSharp
{
	public partial class StringTableEntry : Resource
	{
		#region Fields

		private const string _metadataDelimiter = " ";

		#endregion Fields

		#region Properties

		/// <summary>
		/// The line ID for this line. This value will be the same across all localizations.
		/// </summary>
		[Export]
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// The text of this line, in the language specified by <see cref="Language"/>.
		/// </summary>
		[Export]
		public string Text { get; set; } = string.Empty;

		/// <summary>
		/// The name of the Yarn script in which this line was originally found.
		/// </summary>
		[Export]
		public string File { get; set; } = string.Empty;

		/// <summary>
		/// The name of the node in which this line was originally found.
		/// </summary>
		/// <remarks>This node can be found in the file indicated by <see cref="File"/>.</remarks>
		[Export]
		public string Node { get; set; } = string.Empty;

		/// <summary>
		/// The line number in the file indicated by <see cref="File"/> at which the original
		/// version of this line can be found.
		/// </summary>
		[Export]
		public string LineNumber { get; set; } = string.Empty;

		/// <summary>
		/// A string used as part of a mechanism for checking if translated versions of this string
		/// are out of date.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This field contains the first 8 characters of the SHA-256 hash of the line's text as it
		/// appeared in the base localization CSV file.
		/// </para>
		/// <para>
		/// When a new StringTableEntry is created in a localized CSV file for a .yarn file, the
		/// Lock value is copied over from the base CSV file, and used for the translated entry.
		/// </para>
		/// <para>
		/// Because the base localization CSV is regenerated every time the .yarn file is imported,
		/// the base localization Lock value will change if a line's text changes. This means that
		/// if the base lock and translated lock differ, the translated line is out of date, and
		/// needs to be updated.
		/// </para>
		/// </remarks>
		[Export]
		public string Lock { get; set; } = string.Empty;

		/// <summary>
		/// A comment used to describe this line to translators.
		/// </summary>
		[Export]
		public string Comment { get; set; } = string.Empty;

		/// <summary>
		/// Additional metadata included in this line.
		/// </summary>
		[Export]
		public string[] Metadata { get; set; } = Array.Empty<string>();

		[Export]
		public Godot.Collections.Dictionary<string, string> Translations { get; set; } = new();

		#endregion Properties

		#region Public Constructors

		public StringTableEntry()
		{
		}

		public StringTableEntry(string id, StringInfo stringInfo)
		{
			Id = id;
			Text = stringInfo.text;
			File = stringInfo.fileName;
			Node = stringInfo.nodeName;
			LineNumber = stringInfo.lineNumber.ToString();
			Lock = GetHashString(stringInfo.text, 8);
			Comment = GenerateCommentWithLineMetadata(stringInfo.metadata);
			Metadata = RemoveLineIdFromMetadata(stringInfo.metadata).ToArray();
			Translations = new();
		}

		#endregion Public Constructors

		#region Public Methods

		public static IEnumerable<string> GetPropertyNames()
		{
			return typeof(PropertyName)
				.GetFields(
					System.Reflection.BindingFlags.Public
					| System.Reflection.BindingFlags.Static
					| System.Reflection.BindingFlags.DeclaredOnly
				)
				.Select(x => x.GetValue(null).ToString());
		}

		public static IEnumerable<string> GetCsvHeaders(IEnumerable<string> locales = default)
		{
			return GetPropertyNames()
				.Where(x => x != PropertyName.Translations)
				.Concat(locales);
		}

		public string GetTranslatedText()
		{
			string locale = TranslationServer.GetLocale();
			return Translations.TryGetValue(locale, out string text) ? text : Text;
		}

		public static StringTableEntry FromCsvRow(Dictionary<string, string> csvRow)
		{
			var properties = GetPropertyNames();
			var entry = new StringTableEntry();

			foreach (var kvp in csvRow)
			{
				string key = kvp.Key;
				if (key == PropertyName.Translations)
				{
					continue;
				}

				if (key == PropertyName.MetaData)
				{
					entry.Metadata = kvp.Value.Split(_metadataDelimiter);
					continue;
				}

				if (!properties.Contains(key))
				{
					entry.Translations[key] = kvp.Value;
					continue;
				}

				try
				{
					entry.Set(key, kvp.Value);
				}
				catch (Exception e)
				{
					GD.PushError($"can't set StringTableEntry property '{key}'; error = {e.Message}");
				}
			}

			return entry;
		}

		public Dictionary<string, string> ToCsvRow()
		{
			var row = new Dictionary<string, string>();
			var properties = GetPropertyNames();
			foreach (var property in properties)
			{
				if (property == PropertyName.Translations)
				{
					continue;
				}

				if (property == PropertyName.MetaData)
				{
					row[PropertyName.MetaData] = Metadata.Join(_metadataDelimiter);
					continue;
				}

				row[property] = Get(property).ToString();
			}

			foreach (var kvp in Translations)
			{
				row[kvp.Key] = kvp.Value;
			}

			return row;
		}

		#endregion Public Methods

		#region Private Methods

		/// <summary>
		/// Returns a byte array containing a SHA-256 hash of <paramref name="inputString"/>.
		/// </summary>
		/// <param name="inputString">The string to produce a hash value for.</param>
		/// <returns>The hash of <paramref name="inputString"/>.</returns>
		private static string GetHashString(string inputString, int limitCharacters = -1)
		{
			static byte[] GetHash(string inputString)
			{
				using var algorithm = System.Security.Cryptography.SHA256.Create();
				return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
			}

			var sb = new StringBuilder();
			foreach (byte b in GetHash(inputString))
			{
				sb.Append(b.ToString("x2"));
			}

			if (limitCharacters == -1)
			{
				// Return the entire string
				return sb.ToString();
			}
			else
			{
				// Return a substring (or the entire string, if limitCharacters is longer than the string)
				return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
			}
		}

		private static IEnumerable<string> RemoveLineIdFromMetadata(string[] metadata)
		{
			return metadata.Where(x => !x.StartsWith("line:"));
		}

		private static string GenerateCommentWithLineMetadata(string[] metadata)
		{
			var cleanedMetadata = RemoveLineIdFromMetadata(metadata);
			return cleanedMetadata.Any()
				? $"Line metadata: {string.Join(" ", cleanedMetadata)}"
				: string.Empty;
		}

		#endregion Private Methods
	}
}
