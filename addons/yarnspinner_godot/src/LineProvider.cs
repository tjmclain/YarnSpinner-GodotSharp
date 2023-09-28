using System.Collections.Generic;
using Godot;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class LineProvider : Resource
	{
		private bool _linesAvailable = false;

		[Signal]
		public delegate void LinesBecameAvailableEventHandler();

		[Export]
		public bool UseGodotTranslations { get; set; } = false;

		public StringTable StringTable { get; set; }

		public bool LinesAvailable
		{
			get => _linesAvailable;
			protected set
			{
				_linesAvailable = value;
				if (value)
				{
					EmitSignal(SignalName.LinesBecameAvailable);
				}
			}
		}

		public virtual void PrepareForLines(IEnumerable<string> lineIds)
		{
			// Do nothing by default
			LinesAvailable = true;
		}

		public virtual LocalizedLine GetLocalizedLine(Line line)
		{
			if (StringTable == null)
			{
				GD.PushError("GetLocalizedLine: StringTable == null");
				return LocalizedLine.Empty;
			}

			if (!StringTable.TryGetValue(line.ID, out var entry))
			{
				GD.PushError($"!StringTable.TryGetValue: {line.ID}");
				return LocalizedLine.Empty;
			}

			string text = UseGodotTranslations
				? TranslationServer.Translate(line.ID)
				: entry.GetTranslation();

			return new LocalizedLine()
			{
				TextID = line.ID,
				RawText = text,
				Asset = GetAsset(line.ID),
				Substitutions = line.Substitutions,
				Metadata = entry.Metadata,
			};
		}

		protected virtual Resource GetAsset(string lineId)
		{
			// Do not return an asset
			return null;
		}
	}
}
