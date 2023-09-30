using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class StringTableEntry : Resource
	{
		private const string _delimeter = ", ";

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

		[Export]
		public string[] Metadata { get; set; } = Array.Empty<string>();

		[Export]
		public Godot.Collections.Dictionary<string, string> Translations = new();

		[Export]
		public Godot.Collections.Dictionary<string, string> CustomFields = new();

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
			var properties = new Dictionary<string, Godot.Collections.Dictionary>();
			foreach (var property in GetPropertyList())
			{
				string name = property["name"].AsString();
				properties[name] = property;
			}

			foreach (var kvp in values)
			{
				if (string.IsNullOrEmpty(kvp.Value))
				{
					continue;
				}

				if (properties.TryGetValue(kvp.Key, out var property))
				{
					var type = property["type"].As<Variant.Type>();
					switch (type)
					{
						case Variant.Type.String:
							Set(kvp.Key, kvp.Value);
							break;

						case Variant.Type.PackedStringArray:
							Set(kvp.Key, kvp.Value.Split(_delimeter));
							break;

						default:
							Set(kvp.Key, GD.StrToVar(kvp.Value));
							break;
					}

					continue;
				}

				if (IsLocaleCode(kvp.Key))
				{
					Translations[kvp.Key] = kvp.Value;
					continue;
				}

				CustomFields[kvp.Key] = kvp.Value;
			}
		}

		#endregion Public Constructors

		public string GetTranslation()
			=> GetTranslation(TranslationServer.GetLocale());

		public string GetTranslation(string locale)
			=> Translations.TryGetValue(locale, out string value) ? value : Text;

		public virtual void MergeFrom(StringTableEntry other)
		{
			if (other == null)
			{
				GD.PushError("MergeTranslationsFrom: other == null");
				return;
			}

			bool lockMismatch = Lock != other.Lock;
			if (lockMismatch)
			{
				GD.PushWarning($"StringTableEntry.MergeFrom: Lock mismatch! ",
					"Translations may be out of date. ",
					$"Adding '#lock:{other.Lock}' tags to translated strings; ",
					"remove these manually if strings do not require updates. ",
					$"(line id = {Id}, {Lock} != {other.Lock})");
			}

			foreach (var kvp in other.Translations)
			{
				string locale = kvp.Key;
				string value = kvp.Value;
				if (lockMismatch)
				{
					value += $" #lock:{other.Lock}";
				}

				Translations[locale] = value;
			}

			foreach (var kvp in other.CustomFields)
			{
				CustomFields[kvp.Key] = kvp.Value;
			}
		}

		#region CSV Import / Export

		public string[] ToCsvLine(IEnumerable<string> csvHeaders)
		{
			var properties = GetPropertyList();
			var propertyNames = GetPropertyList()
				.Select(x => x["name"].AsString());

			var line = new List<string>();
			foreach (var key in csvHeaders)
			{
				if (Translations.TryGetValue(key, out string value))
				{
					line.Add(value);
					continue;
				}

				if (propertyNames.Contains(key))
				{
					var variant = Get(key);
					switch (variant.VariantType)
					{
						case Variant.Type.String:
							value = variant.AsString();
							break;

						case Variant.Type.PackedStringArray:
							value = string.Join(_delimeter, variant.AsStringArray());
							break;

						default:
							value = GD.VarToStr(variant);
							break;
					}

					line.Add(value);
					continue;
				}

				if (CustomFields.TryGetValue(key, out value))
				{
					line.Add(value);
					continue;
				}

				line.Add(string.Empty);
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
