using System;
using Godot;

namespace Yarn.GodotSharp.LineProviders
{
	public partial class TextLineProvider : LineProviderBehaviour
	{
		public override LocalizedLine GetLocalizedLine(Line line)
		{
			string text = string.Empty;
			string[] metadata = Array.Empty<string>();

			var stringTable = YarnProject.StringTable;
			if (stringTable.TryGetValue(line.ID, out var entry))
			{
				text = entry.text;
				metadata = entry.metadata;
			}

			if (GodotUtility.TryTranslateString(line.ID, out var translation))
			{
				text = translation;
			}

			return new LocalizedLine()
			{
				TextID = line.ID,
				RawText = text,
				Substitutions = line.Substitutions,
				Metadata = metadata,
			};
		}
	}
}
