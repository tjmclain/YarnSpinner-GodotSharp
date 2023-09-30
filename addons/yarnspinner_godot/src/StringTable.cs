using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class StringTable : Resource, IDictionary<string, StringTableEntry>
	{
		[Export]
		public string[] CsvHeaders { get; set; } = Array.Empty<string>();

		[Export]
		public Godot.Collections.Dictionary<string, StringTableEntry> Entries { get; set; } = new();

		#region IDictionary<string, StringTableEntry>

		public ICollection<string> Keys => Entries.Keys;

		public ICollection<StringTableEntry> Values => Entries.Values;

		public int Count => Entries.Count;

		public bool IsReadOnly => Entries.IsReadOnly;

		public StringTableEntry this[string key]
		{
			get => Entries[key];
			set => Entries[key] = value;
		}

		public bool ContainsKey(string key) => Entries.ContainsKey(key);

		public bool TryGetValue(string key, out StringTableEntry entry)
			=> Entries.TryGetValue(key, out entry);

		public void Add(string key, StringTableEntry value)
			=> Entries.ContainsKey(key);

		public bool Remove(string key)
			=> Entries.Remove(key);

		public void Add(KeyValuePair<string, StringTableEntry> item)
			=> Entries.Add(item.Key, item.Value);

		public bool Contains(KeyValuePair<string, StringTableEntry> item)
			=> Entries.Contains(item);

		public void CopyTo(KeyValuePair<string, StringTableEntry>[] array, int arrayIndex)
		{
			foreach (var kvp in Entries)
			{
				array[arrayIndex] = kvp;
				arrayIndex++;
			}
		}

		public bool Remove(KeyValuePair<string, StringTableEntry> item)
			=> Entries.Remove(item.Key);

		public IEnumerator<KeyValuePair<string, StringTableEntry>> GetEnumerator()
			=> Entries.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> Entries.GetEnumerator();

		public void Clear() => Entries.Clear();

		#endregion IDictionary<string, StringTableEntry>

		public bool TryGetTranslation(string key, out string value)
			=> TryGetTranslation(key, TranslationServer.GetLocale(), out value);

		public bool TryGetTranslation(string key, string locale, out string value)
		{
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
					entry.MergeFrom(existingEntry);
				}
				this[kvp.Key] = entry;
			}
		}

		public void MergeFrom(StringTable other)
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
					entry.MergeFrom(kvp.Value);
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
