using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class StringTable : Resource
	{
		[Export]
		public bool UseGodotTranslations { get; set; } = false;

		[Export]
		public string[] CsvHeaders { get; set; } = System.Array.Empty<string>();

		[Export]
		public Godot.Collections.Dictionary<string, StringTableEntry> Entries { get; set; } = new();

		#region IDictionary<string, StringTableEntry>

		public ICollection<string> Keys => Entries.Keys;

		public ICollection<StringTableEntry> Values => Entries.Values;

		public StringTableEntry this[string key]
		{
			get => Entries[key];
			set => Entries[key] = value;
		}

		public bool TryGetValue(string key, out StringTableEntry entry)
			=> Entries.TryGetValue(key, out entry);

		#endregion IDictionary<string, StringTableEntry>

		public void Clear() => Entries.Clear();

		public bool TryGetTranslation(string key, out string value)
			=> TryGetTranslation(key, TranslationServer.GetLocale(), out value);

		public bool TryGetTranslation(string key, string locale, out string value)
		{
			if (UseGodotTranslations)
			{
				value = TranslationServer.Translate(key);
				return true;
			}

			if (!TryGetValue(key, out var entry))
			{
				GD.PushError($"!Table.TryGetValue '{key}'");
				value = string.Empty;
				return false;
			}

			value = entry.GetTranslation(locale);
			return true;
		}

		public void CreateEntriesFrom(IDictionary<string, Compiler.StringInfo> stringInfo)
		{
			foreach (var kvp in stringInfo)
			{
				var entry = new StringTableEntry(kvp.Key, kvp.Value);
				if (TryGetValue(kvp.Key, out var existingEntry))
				{
					entry.MergeTranslationsFrom(existingEntry);
				}
				this[kvp.Key] = entry;
			}
		}

		public void MergeTranslationsFrom(StringTable other)
		{
			if (other == null)
			{
				GD.PushError("other == null");
				return;
			}

			foreach (var kvp in other.Entries)
			{
				if (TryGetValue(kvp.Key, out var entry))
				{
					entry.MergeTranslationsFrom(kvp.Value);
					continue;
				}

				this[kvp.Key] = kvp.Value;
			}
		}

		#region CSV Import / Export

		public static IEnumerable<string> GetDefaultCsvHeaders()
		{
			return typeof(StringTableEntry)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(x => x.GetCustomAttribute<ExportAttribute>() != null)
				.Select(x => x.Name)
				.Where(x => x != nameof(StringTableEntry.Translations)); // don't include 'Translations' in headers
		}

		#endregion CSV Import / Export
	}
}
