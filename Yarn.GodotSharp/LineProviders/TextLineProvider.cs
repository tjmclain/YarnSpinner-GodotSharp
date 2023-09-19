using System;

namespace Yarn.GodotSharp.LineProviders
{
	public partial class TextLineProvider : LineProviderBehaviour
	{
		public override LocalizedLine GetLocalizedLine(Line line)
		{
			var text = Tr(line.ID);
			var entries = YarnProject.StringTable;
			string[] metadata = entries.TryGetValue(line.ID, out var entry)
				? entry.MetaData
				: Array.Empty<string>();

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
