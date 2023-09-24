using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Yarn.Compiler;
using System;

namespace Yarn.GodotSharp.LineProviders
{
	public partial class LineProvider : Resource
	{
		public virtual Task PrepareForLines(Dictionary<string, StringInfo> stringTable)
		{
			throw new NotImplementedException();
		}

		public virtual LocalizedLine GetLocalizedLine(Line line)
		{
			throw new NotImplementedException();
		}
	}
}
