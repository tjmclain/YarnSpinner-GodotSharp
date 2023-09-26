using Godot;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class StringTable : Resource
	{
		[Export]
		public Godot.Collections.Dictionary<string, StringTableEntry> Entries { get; set; } = new();

		public bool TryGetEntry(string key, out StringTableEntry entry)
			=> Entries.TryGetValue(key, out entry);

		public bool TryGetTranslation(string key, out string value)
			=> TryGetTranslation(key, TranslationServer.GetLocale(), out value);

		public bool TryGetTranslation(string key, string locale, out string value)
		{
			if (!Entries.TryGetValue(key, out var entry))
			{
				GD.PushError($"!Table.TryGetValue '{key}'");
				value = string.Empty;
				return false;
			}

			value = entry.GetTranslation(locale);
			return true;
		}
	}
}
