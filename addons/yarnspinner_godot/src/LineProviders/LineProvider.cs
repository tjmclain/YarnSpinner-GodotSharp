using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Yarn.Compiler;
using System;

namespace Yarn.GodotSharp.LineProviders
{
	[GlobalClass]
	public partial class LineProvider : Resource
	{
		private Dictionary<string, StringInfo> _stringTable = new();
		private bool _linesAvailable = false;

		[Signal]
		public delegate void LinesBecameAvailableEventHandler();

		public Dictionary<string, StringInfo> StringTable
		{
			get => _stringTable;
			set
			{
				if (value != null)
				{
					_stringTable = new(value);
				}
				else
				{
					_stringTable.Clear();
				}
			}
		}

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
			throw new NotImplementedException();
		}

		public virtual LocalizedLine GetLocalizedLine(Line line)
		{
			throw new NotImplementedException();
		}
	}
}
