using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Yarn.Compiler;
using System;

namespace Yarn.GodotSharp
{
	[GlobalClass]
	public partial class LineProvider : Resource
	{
		private bool _linesAvailable = false;

		[Signal]
		public delegate void LinesBecameAvailableEventHandler();

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
			if (!StringTable.TryGetEntry(line.ID, out var entry))
			{
				GD.PushError($"!StringTable.TryGetValue: {line.ID}");
				return LocalizedLine.Empty;
			}

			string text = entry.Text;
			string[] metadata = entry.Metadata;

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
