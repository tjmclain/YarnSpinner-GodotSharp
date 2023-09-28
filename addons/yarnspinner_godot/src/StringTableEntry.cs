using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class StringTableEntry : Resource
	{
		private const string _metadataDelimeter = " ";

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

		[Export]
		public Godot.Collections.Dictionary<string, string> Translations = new();

		public string[] Metadata { get; set; } = Array.Empty<string>();

		#region Public Constructors

		public StringTableEntry()
		{
		}

		public StringTableEntry(string id, StringInfo stringInfo)
		{
			Id = id;
			Text = stringInfo.text;
			Lock = GetHashString(stringInfo.text, 8);
			Metadata = stringInfo.metadata;
		}

		public StringTableEntry(IDictionary<string, string> values)
		{
			const string propertyNameKey = "name";

			// Set properties
			var properties = GetPropertyList();
			foreach (var property in properties)
			{
				string propertyName = property[propertyNameKey].AsString();
				if (propertyName == PropertyName.Metadata)
				{
					continue;
				}

				if (values.TryGetValue(propertyName, out string value))
				{
					Set(propertyName, Variant.From(value));
				}
			}

			// Parse Metadata from string
			if (values.TryGetValue(PropertyName.Metadata, out string metadata))
			{
				Metadata = metadata.Split(_metadataDelimeter);
			}

			// Set translations
			foreach (var kvp in values)
			{
				if (!IsLocaleCode(kvp.Key))
				{
					continue;
				}

				Translations[kvp.Key] = kvp.Value;
			}
		}

		#endregion Public Constructors

		public string GetTranslation(string locale)
		{
			return Translations.TryGetValue(locale, out string value) ? value : Text;
		}

		public void MergeTranslationsFrom(StringTableEntry other)
		{
			if (other == null)
			{
				GD.PushError("MergeTranslationsFrom: other == null");
				return;
			}

			var translations = other.Translations;
			if (translations == null)
			{
				GD.PushError("MergeTranslationsFrom: translations == null");
				return;
			}

			bool lockMismatch = Lock != other.Lock;
			if (lockMismatch)
			{
				GD.PushWarning($"Lock mismatch. Translations may be out of date. line id = {Id}, {Lock} != {other.Lock}");
			}

			foreach (var kvp in translations)
			{
				string locale = kvp.Key;
				if (!IsLocaleCode(locale))
				{
					continue;
				}

				string value = kvp.Value;
				if (lockMismatch)
				{
					// don't remove the translation if there's a mismatch; just append a metadata tag
					value += $" #lock={other.Lock}";
				}

				Translations[locale] = value;
			}
		}

		#region CSV Import / Export

		public string[] ToCsvLine(IEnumerable<string> csvHeaders)
		{
			const string propertyNameKey = "name";

			var properties = GetPropertyList();

			var line = new List<string>();
			foreach (var key in csvHeaders)
			{
				if (Translations.TryGetValue(key, out string value))
				{
					line.Add(value);
					continue;
				}

				foreach (var property in properties)
				{
					string propertyName = property[propertyNameKey].AsString();
					if (propertyName == key)
					{
						value = Get(propertyName).ToString();
						line.Add(value);
						continue;
					}
				}

				// couldn't find key
				// the 'key' in this case could be a locale id for a translation we don't have,
				// so this result is totally fine / not something we should print a warning for
			}

			return line.ToArray();
		}

		#endregion CSV Import / Export

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

		private static bool IsLocaleCode(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			var locales = TranslationServer.GetAllLanguages();
			return locales.Contains(value);
		}
	}
}
