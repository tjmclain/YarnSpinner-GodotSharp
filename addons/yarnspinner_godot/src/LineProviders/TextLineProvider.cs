using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotSharp.LineProviders
{
	[GlobalClass]
	public partial class TextLineProvider : LineProvider
	{
		protected virtual Dictionary<string, StringInfo> StringTable { get; set; } = new();

		public override async Task PrepareForLines(Dictionary<string, StringInfo> stringTable)
		{
			if (stringTable == null)
			{
				GD.PushError("stringTable == null");
				StringTable.Clear();
				return;
			}

			StringTable = new(stringTable);
			await Task.CompletedTask;
		}

		public override LocalizedLine GetLocalizedLine(Line line)
		{
			if (!StringTable.TryGetValue(line.ID, out var entry))
			{
				GD.PushError($"!StringTable.TryGetValue: {line.ID}");
				return LocalizedLine.Empty;
			}

			string text = entry.text;
			string[] metadata = entry.metadata;

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
