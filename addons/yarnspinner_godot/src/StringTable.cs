using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Yarn.GodotSharp
{
	public partial class StringTable : Resource, IDictionary<string, StringTableEntry>
	{
		#region Properties

		[Export]
		public Godot.Collections.Dictionary<string, StringTableEntry> Table { get; set; } = new();

		#endregion Properties

		#region Indexers

		public StringTableEntry this[string key]
		{
			get => Table[key];
			set => Table[key] = value;
		}

		#endregion Indexers

		#region IDictionary Properties

		public ICollection<string> Keys => Table.Keys;

		public ICollection<StringTableEntry> Values => Table.Values;

		public int Count => Table.Count;

		public bool IsReadOnly => Table.IsReadOnly;

		#endregion IDictionary Properties

		#region IDictionary Methods

		public void Add(string key, StringTableEntry value)
		{
			Table.Add(key, value);
		}

		public void Add(KeyValuePair<string, StringTableEntry> item)
		{
			Table.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			Table.Clear();
		}

		public bool Contains(KeyValuePair<string, StringTableEntry> item)
		{
			return Table.Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return Table.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, StringTableEntry>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<string, StringTableEntry>> GetEnumerator()
		{
			return Table.GetEnumerator();
		}

		public bool Remove(string key)
		{
			return Table.Remove(key);
		}

		public bool Remove(KeyValuePair<string, StringTableEntry> item)
		{
			return Table.Remove(item.Key);
		}

		public bool TryGetValue(string key, out StringTableEntry value)
		{
			return Table.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Table.GetEnumerator();
		}

		#endregion IDictionary Methods
	}
}
