using System;
using Godot;

namespace Yarn.GodotSharp.LineProviders
{
	public partial class TextLineProvider : LineProviderBehaviour
	{
		public override LocalizedLine GetLocalizedLine(Line line)
		{
			string fallbackText = string.Empty;
			string[] metadata = Array.Empty<string>();

			var stringTable = YarnProject.StringTable;
			if (stringTable.TryGetValue(line.ID, out var entry))
			{
				fallbackText = entry.Text;
				metadata = entry.MetaData;
			}

			var text = Tr(line.ID);
			if (text == line.ID)
			{
				text = fallbackText;
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