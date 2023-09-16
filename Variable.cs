using Godot;
using System;

namespace Yarn.GodotEngine
{
	public partial class Variable : RefCounted
	{
		[Export]
		public string Name = "";

		[Export]
		public VariableType Type = VariableType.String;

		[Export]
		public string Value = "";

		public float ToNumber()
		{
			return float.Parse(Value);
		}

		public bool ToBoolean()
		{
			return bool.Parse(Value);
		}
	}
}
