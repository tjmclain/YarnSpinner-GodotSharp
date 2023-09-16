using Godot;
using System;

namespace Yarn.GodotEngine
{
	public partial class YarnVariable : RefCounted
	{
		[Export]
		public string Name = "";

		[Export]
		public YarnVariableType Type = YarnVariableType.String;

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